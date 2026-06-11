using System.Collections.Concurrent;
using System.Diagnostics;
using Application.Common.Integrations;
using Application.Common.Settings;
using Domain.Enums;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>
/// Sandbox Huawei IN/OCS gateway — latency, timeout, and balance simulation driven by Global Settings.
/// </summary>
public sealed class HuaweiIntelligentNetworkMockService : IIntelligentNetworkService
{
    private static readonly ConcurrentDictionary<string, decimal> Balances = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> LifecycleStates = new(StringComparer.OrdinalIgnoreCase);

    private readonly IGlobalSettingsProvider _settings;
    private readonly ITelecomIntegrationLogWriter _integrationLog;
    private readonly IntegrationEnablement _integrations;
    private readonly ILogger<HuaweiIntelligentNetworkMockService> _logger;

    public HuaweiIntelligentNetworkMockService(
        IGlobalSettingsProvider settings,
        ITelecomIntegrationLogWriter integrationLog,
        IntegrationEnablement integrations,
        ILogger<HuaweiIntelligentNetworkMockService> logger)
    {
        _settings = settings;
        _integrationLog = integrationLog;
        _integrations = integrations;
        _logger = logger;
    }

    public Task<InOperationResult> ProvisionPrepaidSubscriberAsync(
        string msisdn,
        string imsi,
        string initialProfileId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "ProvisionPrepaidSubscriber",
            msisdn,
            $"imsi={imsi};profile={initialProfileId}",
            () =>
            {
                LifecycleStates[msisdn] = "Active";
                Balances.TryAdd(msisdn, 0m);
                return Task.FromResult(new InOperationResult(
                    true,
                    $"Huawei IN: prepaid subscriber provisioned ({initialProfileId}).",
                    Balances[msisdn]));
            },
            cancellationToken);

    public Task<InOperationResult> GetPrepaidBalanceAsync(
        string msisdn,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "GetPrepaidBalance",
            msisdn,
            null,
            () =>
            {
                var balance = Balances.GetValueOrDefault(msisdn, 0m);
                return Task.FromResult(new InOperationResult(true, $"Balance: {balance:N2}", balance));
            },
            cancellationToken);

    public Task<InOperationResult> CreditPrepaidBalanceAsync(
        string msisdn,
        decimal amount,
        string transactionId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "CreditPrepaidBalance",
            msisdn,
            $"amount={amount};tx={transactionId}",
            () =>
            {
                var balance = Balances.AddOrUpdate(msisdn, amount, (_, current) => current + amount);
                return Task.FromResult(new InOperationResult(
                    true,
                    $"Huawei IN: credited {amount:N2} (tx {transactionId}).",
                    balance));
            },
            cancellationToken);

    public Task<InOperationResult> DebitPrepaidBalanceAsync(
        string msisdn,
        decimal amount,
        string usageType,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "DebitPrepaidBalance",
            msisdn,
            $"amount={amount};usage={usageType}",
            async () =>
            {
                var simulate = await _settings.GetBoolAsync(
                    GlobalSettingKeys.IntegrationInSimulateRealTimeDeduction,
                    true,
                    cancellationToken);

                if (!simulate)
                {
                    return new InOperationResult(true, "Real-time deduction simulation disabled — skipped.", null);
                }

                var current = Balances.GetValueOrDefault(msisdn, 0m);
                if (current < amount)
                {
                    return new InOperationResult(false, $"Insufficient prepaid balance ({current:N2} < {amount:N2}).");
                }

                var balance = Balances.AddOrUpdate(msisdn, 0m, (_, b) => b - amount);
                return new InOperationResult(true, $"Huawei IN: debited {amount:N2} ({usageType}).", balance);
            },
            cancellationToken);

    public Task<InOperationResult> UpdatePrepaidLifecycleStateAsync(
        string msisdn,
        string newState,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UpdatePrepaidLifecycleState",
            msisdn,
            $"state={newState}",
            () =>
            {
                LifecycleStates[msisdn] = newState;
                return Task.FromResult(new InOperationResult(true, $"Huawei IN: lifecycle → {newState}."));
            },
            cancellationToken);

    private async Task<InOperationResult> ExecuteAsync(
        string operationName,
        string msisdn,
        string? requestDetail,
        Func<Task<InOperationResult>> action,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var gatewayUrl = await _settings.GetValueAsync(GlobalSettingKeys.IntegrationInGatewayUrl, cancellationToken)
            ?? "https://in-gateway.syriatel.local/api/v1";
        var timeoutMs = await _settings.GetIntAsync(
            GlobalSettingKeys.IntegrationInTimeoutMilliseconds,
            5000,
            500,
            60000,
            cancellationToken);

        var requestPayload = $"POST {gatewayUrl}/{operationName}; msisdn={msisdn}; {requestDetail}";

        if (!await _integrations.IsInEnabledAsync(cancellationToken))
        {
            var fallbackPayload = $"{IntegrationCircuitBreaker.FallbackNotesEn} | {IntegrationCircuitBreaker.FallbackMessageAr}";
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_IN,
                operationName,
                msisdn,
                requestPayload,
                fallbackPayload,
                true,
                IntegrationCircuitBreaker.ResponseStatusFallback,
                sw.ElapsedMilliseconds,
                cancellationToken);
            return new InOperationResult(true, IntegrationCircuitBreaker.FallbackMessageAr, QueuedForSync: true);
        }

        try
        {
            await SimulateNetworkLatencyAsync(timeoutMs, msisdn, cancellationToken);

            if (ShouldSimulateTimeout(msisdn, timeoutMs))
            {
                var timeoutMsg = $"Huawei IN timeout after {timeoutMs}ms (simulated).";
                await _integrationLog.WriteAsync(
                    TelecomIntegrationSystem.Huawei_IN,
                    operationName,
                    msisdn,
                    requestPayload,
                    timeoutMsg,
                    false,
                    "TIMEOUT",
                    sw.ElapsedMilliseconds,
                    cancellationToken);
                return new InOperationResult(false, timeoutMsg);
            }

            if (ShouldSimulateFailure(msisdn, operationName))
            {
                var failMsg = "Huawei IN rating node rejected request (simulated fallout).";
                await _integrationLog.WriteAsync(
                    TelecomIntegrationSystem.Huawei_IN,
                    operationName,
                    msisdn,
                    requestPayload,
                    failMsg,
                    false,
                    "FALLOUT",
                    sw.ElapsedMilliseconds,
                    cancellationToken);
                return new InOperationResult(false, failMsg);
            }

            var result = await action();
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_IN,
                operationName,
                msisdn,
                requestPayload,
                result.Message,
                result.Success,
                result.Success ? "OK" : "ERROR",
                sw.ElapsedMilliseconds,
                cancellationToken);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var timeoutMsg = $"Huawei IN gateway timeout ({timeoutMs}ms).";
            _logger.LogWarning("IN mock timeout for {Msisdn} op {Operation}", msisdn, operationName);
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_IN,
                operationName,
                msisdn,
                requestPayload,
                timeoutMsg,
                false,
                "TIMEOUT",
                sw.ElapsedMilliseconds,
                cancellationToken);
            return new InOperationResult(false, timeoutMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IN mock error for {Msisdn} op {Operation}", msisdn, operationName);
            await _integrationLog.WriteAsync(
                TelecomIntegrationSystem.Huawei_IN,
                operationName,
                msisdn,
                requestPayload,
                ex.Message,
                false,
                "ERROR",
                sw.ElapsedMilliseconds,
                cancellationToken);
            return new InOperationResult(false, ex.Message);
        }
    }

    private static async Task SimulateNetworkLatencyAsync(int timeoutMs, string msisdn, CancellationToken cancellationToken)
    {
        var baseLatency = Math.Clamp(timeoutMs / 8, 80, 2500);
        var jitter = Math.Abs(msisdn.GetHashCode()) % 120;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        await Task.Delay(baseLatency + jitter, cts.Token);
    }

    private static bool ShouldSimulateTimeout(string msisdn, int timeoutMs) =>
        timeoutMs <= 1500 && msisdn.EndsWith('7');

    private static bool ShouldSimulateFailure(string msisdn, string operationName)
    {
        if (operationName == "ProvisionPrepaidSubscriber" && msisdn.EndsWith('9'))
        {
            return true;
        }

        return Math.Abs(HashCode.Combine(msisdn, operationName)) % 25 == 0;
    }
}
