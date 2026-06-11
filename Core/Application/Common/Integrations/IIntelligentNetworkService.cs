namespace Application.Common.Integrations;

public sealed record InOperationResult(
    bool Success,
    string Message,
    decimal? BalanceAfter = null,
    bool QueuedForSync = false);

/// <summary>
/// Real-time online charging (OCS/IN) for prepaid subscribers — balance, top-up, lifecycle locks.
/// </summary>
public interface IIntelligentNetworkService
{
    Task<InOperationResult> ProvisionPrepaidSubscriberAsync(
        string msisdn,
        string imsi,
        string initialProfileId,
        CancellationToken cancellationToken = default);

    Task<InOperationResult> GetPrepaidBalanceAsync(
        string msisdn,
        CancellationToken cancellationToken = default);

    Task<InOperationResult> CreditPrepaidBalanceAsync(
        string msisdn,
        decimal amount,
        string transactionId,
        CancellationToken cancellationToken = default);

    Task<InOperationResult> DebitPrepaidBalanceAsync(
        string msisdn,
        decimal amount,
        string usageType,
        CancellationToken cancellationToken = default);

    Task<InOperationResult> UpdatePrepaidLifecycleStateAsync(
        string msisdn,
        string newState,
        CancellationToken cancellationToken = default);
}
