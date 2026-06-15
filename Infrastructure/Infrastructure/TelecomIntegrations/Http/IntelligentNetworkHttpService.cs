using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations.Http;

/// <summary>HTTP stub for intelligent network — delegates to mock when simulator unavailable.</summary>
public sealed class IntelligentNetworkHttpService : IIntelligentNetworkService
{
    private readonly HuaweiIntelligentNetworkMockService _fallback;

    public IntelligentNetworkHttpService(HuaweiIntelligentNetworkMockService fallback) => _fallback = fallback;

    public Task<InOperationResult> ProvisionPrepaidSubscriberAsync(
        string msisdn,
        string imsi,
        string initialProfileId,
        CancellationToken cancellationToken = default) =>
        _fallback.ProvisionPrepaidSubscriberAsync(msisdn, imsi, initialProfileId, cancellationToken);

    public Task<InOperationResult> GetPrepaidBalanceAsync(string msisdn, CancellationToken cancellationToken = default) =>
        _fallback.GetPrepaidBalanceAsync(msisdn, cancellationToken);

    public Task<InOperationResult> CreditPrepaidBalanceAsync(
        string msisdn,
        decimal amount,
        string transactionId,
        CancellationToken cancellationToken = default) =>
        _fallback.CreditPrepaidBalanceAsync(msisdn, amount, transactionId, cancellationToken);

    public Task<InOperationResult> DebitPrepaidBalanceAsync(
        string msisdn,
        decimal amount,
        string usageType,
        CancellationToken cancellationToken = default) =>
        _fallback.DebitPrepaidBalanceAsync(msisdn, amount, usageType, cancellationToken);

    public Task<InOperationResult> UpdatePrepaidLifecycleStateAsync(
        string msisdn,
        string newState,
        CancellationToken cancellationToken = default) =>
        _fallback.UpdatePrepaidLifecycleStateAsync(msisdn, newState, cancellationToken);
}
