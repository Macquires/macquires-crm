using System.Diagnostics;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Demo HLR provisioning with Polly retry + circuit breaker.</summary>
public sealed class HlrNetworkProvisioningService : INetworkProvisioningService
{
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly ITelecomIntegrationLogWriter _integrationLog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<TelecomBillingOptions> _options;
    private readonly ILogger<HlrNetworkProvisioningService> _logger;
    private readonly IntegrationEnablement _integrations;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public HlrNetworkProvisioningService(
        ICommandRepository<BillingIntegrationLog> logRepository,
        ITelecomIntegrationLogWriter integrationLog,
        IUnitOfWork unitOfWork,
        IOptions<TelecomBillingOptions> options,
        ILogger<HlrNetworkProvisioningService> logger,
        IntegrationEnablement integrations)
    {
        _logRepository = logRepository;
        _integrationLog = integrationLog;
        _unitOfWork = unitOfWork;
        _options = options;
        _logger = logger;
        _integrations = integrations;
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, duration) =>
                    _logger.LogWarning(ex, "HLR circuit open for {Duration}s", duration.TotalSeconds),
                onReset: () => _logger.LogInformation("HLR circuit reset"));
    }

    public async Task<NetworkProvisionResult> ProvisionAsync(
        NetworkProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Kind == TelecomOperationKind.NewActivation
            && string.IsNullOrWhiteSpace(request.Imsi))
        {
            return new NetworkProvisionResult(
                false,
                "IMSI مطلوب لتزويد HLR (CreateSubscriber).");
        }

        var sw = Stopwatch.StartNew();
        var operationName = OperationNameFor(request.Kind);
        var requestPayload = request.ToString();

        if (!await _integrations.IsHlrEnabledAsync(cancellationToken))
        {
            var fallbackPayload = $"{IntegrationCircuitBreaker.FallbackNotesEn} | {IntegrationCircuitBreaker.FallbackMessageAr}";
            await WriteIntegrationLogAsync(
                request, operationName, requestPayload, fallbackPayload, true, IntegrationCircuitBreaker.ResponseStatusFallback, sw.ElapsedMilliseconds, cancellationToken);
            return new NetworkProvisionResult(
                true,
                IntegrationCircuitBreaker.FallbackMessageAr,
                DeferRetry: true,
                QueuedForSync: true);
        }

        var opt = _options.Value;
        var attempt = 0;

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                opt.MaxRetryAttempts,
                retry => TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs * retry),
                (ex, ts, retryCount, _) =>
                    _logger.LogWarning(ex, "HLR retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds));

        try
        {
            var result = await _circuitBreaker.ExecuteAsync(async () =>
                await retryPolicy.ExecuteAsync(async () =>
                {
                    attempt++;
                    var successMsg =
                        $"{operationName} MSISDN={request.Msisdn} IMSI={request.Imsi} ICCID={request.Iccid} " +
                        $"plan={request.SubscriptionTypeCode ?? "—"}";
                    await WriteBillingLogAsync(
                        request.OperationId,
                        attempt,
                        true,
                        successMsg,
                        "HLR-Mock",
                        request.CorrelationId,
                        requestPayload,
                        "OK",
                        cancellationToken);
                    return new NetworkProvisionResult(true, $"HLR OK (attempt {attempt})");
                }));

            await WriteIntegrationLogAsync(
                request, operationName, requestPayload, result.Message, result.Success, "200", sw.ElapsedMilliseconds, cancellationToken);
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "HLR circuit open — deferring {Number}", request.OperationNumber);
            await WriteBillingLogAsync(
                request.OperationId,
                attempt + 1,
                false,
                "HLR circuit open — queued for retry",
                "HLR-Mock",
                request.CorrelationId,
                requestPayload,
                ex.Message,
                cancellationToken);
            var result = new NetworkProvisionResult(false, "الشبكة مشغولة — سيتم إعادة المحاولة تلقائياً.", DeferRetry: true);
            await WriteIntegrationLogAsync(
                request, operationName, requestPayload, ex.Message, false, "CIRCUIT_OPEN", sw.ElapsedMilliseconds, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HLR exhausted retries for {Number}", request.OperationNumber);
            await WriteBillingLogAsync(
                request.OperationId,
                attempt,
                false,
                ex.Message,
                "HLR-Mock",
                request.CorrelationId,
                requestPayload,
                ex.Message,
                cancellationToken);
            await WriteIntegrationLogAsync(
                request, operationName, requestPayload, ex.Message, false, "ERROR", sw.ElapsedMilliseconds, cancellationToken);
            return new NetworkProvisionResult(false, ex.Message);
        }
    }

    private Task WriteIntegrationLogAsync(
        NetworkProvisionRequest request,
        string operationName,
        string? requestPayload,
        string? responsePayload,
        bool success,
        string statusCode,
        long elapsedMs,
        CancellationToken cancellationToken) =>
        _integrationLog.WriteAsync(
            TelecomIntegrationSystem.Huawei_HLR,
            operationName,
            request.Msisdn,
            requestPayload,
            responsePayload,
            success,
            statusCode,
            elapsedMs,
            cancellationToken);

    private async Task WriteBillingLogAsync(
        string operationId,
        int attempt,
        bool success,
        string message,
        string target,
        string? correlationId,
        string? requestPayload,
        string? responsePayload,
        CancellationToken cancellationToken)
    {
        await _logRepository.CreateAsync(new BillingIntegrationLog
        {
            TelecomOperationRequestId = operationId,
            AttemptNumber = attempt,
            Success = success,
            Message = message,
            IntegrationTarget = target,
            CorrelationId = correlationId,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    internal static string OperationNameFor(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.SimSwap => TelecomBssOperations.HlrSimProfileUpdate,
        TelecomOperationKind.Migration => "Migration_OfferChange",
        TelecomOperationKind.NewActivation => TelecomBssOperations.HlrCreateSubscriber,
        TelecomOperationKind.TakeOver => "TakeOver_OwnershipTransfer",
        _ => $"ActivateLine_{kind}"
    };
}
