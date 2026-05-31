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

/// <summary>Demo VAS provisioning via HLR command templates (Huawei CBS style).</summary>
public sealed class HlrVasProvisioningService : IVasProvisioningService
{
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly ITelecomIntegrationLogWriter _integrationLog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<TelecomBillingOptions> _options;
    private readonly ILogger<HlrVasProvisioningService> _logger;
    private readonly IntegrationEnablement _integrations;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public HlrVasProvisioningService(
        ICommandRepository<BillingIntegrationLog> logRepository,
        ITelecomIntegrationLogWriter integrationLog,
        IUnitOfWork unitOfWork,
        IOptions<TelecomBillingOptions> options,
        ILogger<HlrVasProvisioningService> logger,
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
                    _logger.LogWarning(ex, "VAS HLR circuit open for {Duration}s", duration.TotalSeconds),
                onReset: () => _logger.LogInformation("VAS HLR circuit reset"));
    }

    public async Task<VasProvisionResult> ProvisionVasAsync(VasProvisionRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var operationName = request.Activate ? "VasToggle_On" : "VasToggle_Off";
        var command = request.HlrCommandTemplate
            .Replace("{MSISDN}", request.Msisdn, StringComparison.OrdinalIgnoreCase)
            .Replace("{ACTION}", request.Activate ? "ADD" : "DEL", StringComparison.OrdinalIgnoreCase);

        if (!await _integrations.IsHlrEnabledAsync(cancellationToken))
        {
            var fallbackPayload = $"{IntegrationCircuitBreaker.FallbackNotesEn} | {IntegrationCircuitBreaker.FallbackMessageAr}";
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_HLR,
                operationName,
                request.Msisdn,
                command,
                fallbackPayload,
                true,
                IntegrationCircuitBreaker.ResponseStatusFallback,
                sw.ElapsedMilliseconds,
                cancellationToken);
            return new VasProvisionResult(true, IntegrationCircuitBreaker.FallbackMessageAr, command);
        }

        var opt = _options.Value;
        var attempt = 0;

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                opt.MaxRetryAttempts,
                retry => TimeSpan.FromMilliseconds(opt.RetryBaseDelayMs * retry),
                (ex, ts, retryCount, _) =>
                    _logger.LogWarning(ex, "VAS HLR retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds));

        try
        {
            var result = await _circuitBreaker.ExecuteAsync(async () =>
                await retryPolicy.ExecuteAsync(async () =>
                {
                    attempt++;
                    await WriteBillingLogAsync(
                        request.TelecomOperationRequestId,
                        attempt,
                        true,
                        $"VAS {(request.Activate ? "activate" : "deactivate")} MSISDN={request.Msisdn}",
                        command,
                        request.CorrelationId,
                        cancellationToken);
                    return new VasProvisionResult(true, $"HLR VAS OK (attempt {attempt})", command);
                }));

            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_HLR,
                operationName,
                request.Msisdn,
                command,
                result.Message,
                result.Success,
                "200",
                sw.ElapsedMilliseconds,
                cancellationToken);
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "VAS HLR circuit open for {Msisdn}", request.Msisdn);
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_HLR, operationName, request.Msisdn, command, ex.Message, false, "CIRCUIT_OPEN", sw.ElapsedMilliseconds, cancellationToken);
            return new VasProvisionResult(false, "الشبكة مشغولة — حاول لاحقاً.", command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VAS HLR failed for {Msisdn}", request.Msisdn);
            await WriteBillingLogAsync(
                request.TelecomOperationRequestId,
                attempt + 1,
                false,
                ex.Message,
                command,
                request.CorrelationId,
                cancellationToken);
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_HLR, operationName, request.Msisdn, command, ex.Message, false, "ERROR", sw.ElapsedMilliseconds, cancellationToken);
            return new VasProvisionResult(false, ex.Message, command);
        }
    }

    private async Task WriteBillingLogAsync(
        string telecomOperationRequestId,
        int attempt,
        bool success,
        string message,
        string command,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telecomOperationRequestId))
        {
            return;
        }

        await _logRepository.CreateAsync(new BillingIntegrationLog
        {
            TelecomOperationRequestId = telecomOperationRequestId,
            AttemptNumber = attempt,
            Success = success,
            Message = message,
            IntegrationTarget = "HLR-VAS-Mock",
            CorrelationId = correlationId,
            RequestPayload = command,
            ResponsePayload = success ? "OK" : message
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
