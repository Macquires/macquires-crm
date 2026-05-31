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
        request.Kind == TelecomOperationKind.NewActivation
            ? TelecomBssOperations.CbsCreateAccountProfile
            : $"CbsProvision_{request.Kind}";

    private static string BuildSuccessMessage(BillingProvisionRequest request, int attempt)
    {
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
