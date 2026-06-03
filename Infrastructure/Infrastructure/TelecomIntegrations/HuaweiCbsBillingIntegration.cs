using System.Diagnostics;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Demo Huawei CBS bridge: Polly retries, per-attempt <see cref="BillingIntegrationLog"/> rows.</summary>
public sealed class HuaweiCbsBillingIntegration : IBillingSystemIntegration
{
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly ITelecomIntegrationLogWriter _integrationLog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<TelecomBillingOptions> _options;
    private readonly ILogger<HuaweiCbsBillingIntegration> _logger;
    private readonly IntegrationEnablement _integrations;

    public HuaweiCbsBillingIntegration(
        ICommandRepository<BillingIntegrationLog> logRepository,
        ITelecomIntegrationLogWriter integrationLog,
        IUnitOfWork unitOfWork,
        IOptions<TelecomBillingOptions> options,
        ILogger<HuaweiCbsBillingIntegration> logger,
        IntegrationEnablement integrations)
    {
        _logRepository = logRepository;
        _integrationLog = integrationLog;
        _unitOfWork = unitOfWork;
        _options = options;
        _logger = logger;
        _integrations = integrations;
    }

    public async Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Phase == TelecomBillingProvisionPhase.Reverse)
        {
            return await ReverseProvisionAsync(request, cancellationToken);
        }

        if (await HasSuccessfulProvisionAsync(request.OperationId, cancellationToken))
        {
            return new BillingProvisionResult(
                true,
                "Huawei CBS idempotent replay (already provisioned).",
                IdempotentReplay: true);
        }

        if (!await _integrations.IsHuaweiEnabledAsync(cancellationToken))
        {
            var sw = Stopwatch.StartNew();
            var payload = $"{IntegrationCircuitBreaker.FallbackNotesEn} | {IntegrationCircuitBreaker.FallbackMessageAr}";
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_CBS,
                CbsOperationName(request),
                request.Msisdn,
                request.ToString(),
                payload,
                true,
                IntegrationCircuitBreaker.ResponseStatusFallback,
                sw.ElapsedMilliseconds,
                cancellationToken);
            return new BillingProvisionResult(true, IntegrationCircuitBreaker.FallbackMessageAr, QueuedForSync: true);
        }

        var opt = _options.Value;
        var attempt = 0;

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                opt.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs * retryAttempt),
                onRetry: (ex, ts, retryCount, _) =>
                {
                    _logger.LogWarning(ex, "Huawei CBS mock retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds);
                });

        try
        {
            return await policy.ExecuteAsync(async () =>
            {
                attempt++;
                var failUntil = Math.Max(0, opt.FailAttemptsBeforeSuccess);

                if (attempt <= failUntil)
                {
                    await WriteLogAsync(
                        request,
                        attempt,
                        false,
                        "Huawei CBS timeout (simulated)",
                        opt.IntegrationTarget,
                        cancellationToken);
                    throw new InvalidOperationException("Huawei CBS timeout (simulated)");
                }

                if (opt.EnableChaosEngineering && attempt == 1)
                {
                    var chaosProbability = Random.Shared.Next(1, 100);
                    if (chaosProbability <= 5)
                    {
                        await WriteLogAsync(
                            request,
                            attempt,
                            false,
                            "Simulated Packet Loss: Connection dropped unexpectedly.",
                            opt.IntegrationTarget,
                            cancellationToken);
                        throw new HttpRequestException("Simulated Packet Loss: Connection dropped unexpectedly.");
                    }

                    if (chaosProbability <= 10)
                    {
                        await WriteLogAsync(
                            request,
                            attempt,
                            false,
                            "HTTP 429 Too Many Requests: Huawei CBS Rate Limit Exceeded.",
                            opt.IntegrationTarget,
                            cancellationToken);
                        throw new HttpRequestException("HTTP 429 Too Many Requests: Huawei CBS Rate Limit Exceeded.");
                    }
                }

                var message = BuildSuccessMessage(request, attempt);
                await WriteLogAsync(request, attempt, true, message, opt.IntegrationTarget, cancellationToken);
                return new BillingProvisionResult(true, message);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Huawei CBS mock exhausted retries for {OperationNumber}", request.OperationNumber);
            return new BillingProvisionResult(false, ex.Message);
        }
    }

    public async Task<BillingProvisionResult> ReverseProvisionAsync(
        BillingProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _integrations.IsHuaweiEnabledAsync(cancellationToken))
        {
            return new BillingProvisionResult(true, "Huawei CBS reverse skipped (fallback mode).");
        }

        var message =
            $"{TelecomBssOperations.CbsReverseAccount}: reversed {request.Kind} for {request.OperationNumber} MSISDN={request.Msisdn}";
        await WriteLogAsync(
            request with { Phase = TelecomBillingProvisionPhase.Reverse },
            1,
            true,
            message,
            _options.Value.IntegrationTarget,
            cancellationToken);

        var sw = Stopwatch.StartNew();
        await _integrationLog.WriteAsync(
            TelecomIntegrationSystem.Huawei_CBS,
            TelecomBssOperations.CbsReverseAccount,
            request.Msisdn,
            request.ToString(),
            message,
            true,
            "200",
            sw.ElapsedMilliseconds,
            cancellationToken);

        return new BillingProvisionResult(true, "Huawei CBS compensation (reverse) OK");
    }

    public async Task<BillingRechargeResult> RechargeAsync(
        BillingRechargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Msisdn))
        {
            return new BillingRechargeResult(false, "رقم الخط مطلوب للشحن.");
        }

        if (request.Amount <= 0)
        {
            return new BillingRechargeResult(false, "مبلغ الشحن يجب أن يكون أكبر من صفر.");
        }

        var cleanMsisdn = request.Msisdn.Trim();
        var isValidSyriatel = cleanMsisdn.StartsWith("093") || cleanMsisdn.StartsWith("099");
        if (!isValidSyriatel)
        {
            await WritePaymentRechargeLogAsync(
                request,
                1,
                false,
                $"Recharge failed: Subscriber {cleanMsisdn} Not Found (Code 20001)",
                cancellationToken);
            return new BillingRechargeResult(false, $"فشل الشحن: المشترك {cleanMsisdn} غير موجود في CBS.");
        }

        if (!await _integrations.IsHuaweiEnabledAsync(cancellationToken))
        {
            var fallbackBalance = 5000m + request.Amount;
            await WritePaymentRechargeLogAsync(
                request,
                1,
                true,
                $"{IntegrationCircuitBreaker.FallbackMessageAr} | simulated balance={fallbackBalance:0.##}",
                cancellationToken);
            return new BillingRechargeResult(true, IntegrationCircuitBreaker.FallbackMessageAr, fallbackBalance);
        }

        var opt = _options.Value;
        var attempt = 0;
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                opt.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs * retryAttempt),
                onRetry: (ex, ts, retryCount, _) =>
                {
                    _logger.LogWarning(ex, "Huawei CBS recharge retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds);
                });

        try
        {
            return await policy.ExecuteAsync(async () =>
            {
                attempt++;
                if (attempt <= Math.Max(0, opt.FailAttemptsBeforeSuccess))
                {
                    await WritePaymentRechargeLogAsync(
                        request,
                        attempt,
                        false,
                        "Huawei CBS recharge timeout (simulated)",
                        cancellationToken);
                    throw new InvalidOperationException("Huawei CBS recharge timeout (simulated)");
                }

                var newBalance = Random.Shared.Next(5000, 15000) + request.Amount;
                var message =
                    $"{TelecomBssOperations.CbsRechargeTopUp}: {request.Amount:0.##} SYP to {cleanMsisdn} (PAY {request.PaymentNumber}, attempt {attempt})";
                await WritePaymentRechargeLogAsync(request, attempt, true, message, cancellationToken);
                return new BillingRechargeResult(true, message, newBalance);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Huawei CBS recharge exhausted retries for {PaymentNumber}", request.PaymentNumber);
            return new BillingRechargeResult(false, ex.Message);
        }
    }

    public async Task<BillingRechargeResult> ReverseRechargeAsync(
        BillingReverseRechargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            return new BillingRechargeResult(false, "مبلغ العكس غير صالح.");
        }

        var message =
            $"{TelecomBssOperations.CbsRechargeTopUp} REVERSE: -{request.Amount:0.##} SYP from {request.Msisdn} (PAY {request.PaymentNumber})";

        if (!await _integrations.IsHuaweiEnabledAsync(cancellationToken))
        {
            await WritePaymentRechargeLogAsync(
                new BillingRechargeRequest(
                    request.PaymentTransactionId,
                    request.PaymentNumber,
                    request.Msisdn,
                    -request.Amount,
                    request.CorrelationId),
                1,
                true,
                message + " (fallback)",
                cancellationToken);
            return new BillingRechargeResult(true, IntegrationCircuitBreaker.FallbackMessageAr, 1000m);
        }

        await WritePaymentRechargeLogAsync(
            new BillingRechargeRequest(
                request.PaymentTransactionId,
                request.PaymentNumber,
                request.Msisdn,
                -request.Amount,
                request.CorrelationId),
            1,
            true,
            message,
            cancellationToken);

        return new BillingRechargeResult(true, message, null);
    }

    public async Task<decimal> GetOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        if (msisdn == "0931112223")
        {
            return -50m;
        }

        if (msisdn?.EndsWith("9") == true)
        {
            return -5000m;
        }

        return 1000m;
    }

    private async Task<bool> HasSuccessfulProvisionAsync(string operationId, CancellationToken cancellationToken)
    {
        return await _logRepository.GetQuery()
            .AnyAsync(
                l => !l.IsDeleted
                    && l.TelecomOperationRequestId == operationId
                    && l.Success
                    && l.Message != null
                    && !l.Message.Contains(TelecomBssOperations.CbsReverseAccount, StringComparison.Ordinal),
                cancellationToken);
    }

    private static string CbsOperationName(BillingProvisionRequest request) =>
        request.Kind switch
        {
            TelecomOperationKind.NewActivation => TelecomBssOperations.CbsCreateAccountProfile,
            TelecomOperationKind.ChangeGsmType => TelecomBssOperations.CbsChangeServiceType,
            TelecomOperationKind.TakeOver => TelecomBssOperations.CbsTransferOwnership,
            TelecomOperationKind.SimSwap => TelecomBssOperations.CbsSimProfileUpdate,
            TelecomOperationKind.NumberPortability => TelecomBssOperations.CbsMsisdnReassign,
            TelecomOperationKind.Termination => TelecomBssOperations.CbsGenerateFinalBill,
            TelecomOperationKind.Migration => TelecomBssOperations.CbsChangePrimaryOffer,
            TelecomOperationKind.TemporarySuspension => TelecomBssOperations.CbsBarSubscriber,
            TelecomOperationKind.Reconnect => TelecomBssOperations.CbsUnbarSubscriber,
            _ => $"CbsProvision_{request.Kind}",
        };

    private static string BuildSuccessMessage(BillingProvisionRequest request, int attempt)
    {
        if (request.Kind == TelecomOperationKind.ChangeGsmType)
        {
            return
                $"{TelecomBssOperations.CbsChangeServiceType} MSISDN={request.Msisdn} " +
                $"type={request.SubscriptionTypeCode ?? "—"} (attempt {attempt})";
        }

        if (request.Kind == TelecomOperationKind.TakeOver)
        {
            return
                $"{TelecomBssOperations.CbsTransferOwnership} MSISDN={request.Msisdn} (attempt {attempt})";
        }

        if (request.Kind == TelecomOperationKind.SimSwap)
        {
            return
                $"{TelecomBssOperations.CbsSimProfileUpdate} MSISDN={request.Msisdn} " +
                $"new={request.Iccid ?? "—"} prior={request.PriorIccid ?? "—"} (attempt {attempt})";
        }

        if (request.Kind == TelecomOperationKind.NumberPortability)
        {
            return
                $"{TelecomBssOperations.CbsMsisdnReassign} new={request.Msisdn} " +
                $"prior={request.PriorMsisdn ?? "—"} (attempt {attempt})";
        }

        if (request.Kind == TelecomOperationKind.Termination)
        {
            return
                $"{TelecomBssOperations.CbsGenerateFinalBill} MSISDN={request.Msisdn} finalBill=12500 (attempt {attempt})";
        }

        if (request.Kind == TelecomOperationKind.Migration)
        {
            return
                $"{TelecomBssOperations.CbsChangePrimaryOffer} MSISDN={request.Msisdn} " +
                $"service={request.ProductServiceCode ?? "—"} (attempt {attempt})";
        }

        if (request.Kind == TelecomOperationKind.TemporarySuspension)
        {
            return $"{TelecomBssOperations.CbsBarSubscriber} MSISDN={request.Msisdn} (attempt {attempt})";
        }

        if (request.Kind == TelecomOperationKind.Reconnect)
        {
            return $"{TelecomBssOperations.CbsUnbarSubscriber} MSISDN={request.Msisdn} (attempt {attempt})";
        }

        if (request.Kind != TelecomOperationKind.NewActivation)
        {
            return $"Provisioned {request.Kind} for {request.OperationNumber} (attempt {attempt})";
        }

        var deposit = request.InitialDeposit.HasValue
            ? $" deposit={request.InitialDeposit.Value:0.##}"
            : string.Empty;
        return
            $"{TelecomBssOperations.CbsCreateAccountProfile} + {TelecomBssOperations.CbsPostInitialDeposit}{deposit} " +
            $"MSISDN={request.Msisdn} service={request.ProductServiceCode ?? "—"} (attempt {attempt})";
    }

    private async Task WritePaymentRechargeLogAsync(
        BillingRechargeRequest request,
        int attemptNumber,
        bool success,
        string message,
        CancellationToken cancellationToken)
    {
        var row = new BillingIntegrationLog
        {
            TelecomPaymentTransactionId = request.PaymentTransactionId,
            AttemptNumber = attemptNumber,
            Success = success,
            Message = message,
            IntegrationTarget = _options.Value.IntegrationTarget,
            CorrelationId = request.CorrelationId,
            RequestPayload = request.ToString(),
            ResponsePayload = success ? "OK" : message
        };

        await _logRepository.CreateAsync(row, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _integrationLog.WriteAsync(
            TelecomIntegrationSystem.Huawei_CBS,
            TelecomBssOperations.CbsRechargeTopUp,
            request.Msisdn,
            request.ToString(),
            message,
            success,
            success ? "200" : "500",
            sw.ElapsedMilliseconds,
            cancellationToken);
    }

    private async Task WriteLogAsync(
        BillingProvisionRequest request,
        int attemptNumber,
        bool success,
        string message,
        string? integrationTarget,
        CancellationToken cancellationToken)
    {
        var row = new BillingIntegrationLog
        {
            TelecomOperationRequestId = request.OperationId,
            AttemptNumber = attemptNumber,
            Success = success,
            Message = message,
            IntegrationTarget = integrationTarget,
            CorrelationId = request.CorrelationId,
            RequestPayload = request.ToString(),
            ResponsePayload = success ? "OK" : message
        };

        await _logRepository.CreateAsync(row, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
