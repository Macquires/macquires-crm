using Domain.Enums;

namespace Application.Common.Integrations;

/// <summary>
/// HLR provisioning payload. Infrastructure maps <see cref="Kind"/> to
/// <see cref="TelecomBssOperations.HlrCreateSubscriber"/> and related operations.
/// </summary>
public sealed record NetworkProvisionRequest(
    string OperationId,
    string OperationNumber,
    string? Msisdn,
    string? Iccid,
    string? CorrelationId,
    TelecomOperationKind Kind,
    string? Imsi = null,
    string? ProductServiceCode = null,
    string? SubscriptionTypeCode = null,
    string? PriorMsisdn = null,
    string? BranchId = null);

public sealed record NetworkProvisionResult(
    bool Success,
    string Message,
    bool DeferRetry = false,
    bool QueuedForSync = false);

/// <summary>HLR / network activation bridge (implemented in Infrastructure).</summary>
public interface INetworkProvisioningService
{
    Task<NetworkProvisionResult> ProvisionAsync(NetworkProvisionRequest request, CancellationToken cancellationToken = default);
}
