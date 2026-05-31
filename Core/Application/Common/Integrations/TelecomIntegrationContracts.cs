using Domain.Enums;

namespace Application.Common.Integrations;

/// <summary>
/// CBS provisioning / compensation payload. Infrastructure maps <see cref="Kind"/> to
/// <see cref="TelecomBssOperations"/> (CreateAccountProfile, initial deposit, reverse).
/// </summary>
public sealed record BillingProvisionRequest(
    string OperationId,
    string OperationNumber,
    string? Msisdn,
    TelecomOperationKind Kind,
    string? CorrelationId = null,
    decimal? InitialDeposit = null,
    string? ProductServiceCode = null,
    string? SubscriptionTypeCode = null,
    string? Imsi = null,
    string? Iccid = null,
    TelecomBillingProvisionPhase Phase = TelecomBillingProvisionPhase.Provision);

public sealed record BillingProvisionResult(
    bool Success,
    string Message,
    bool QueuedForSync = false,
    bool IdempotentReplay = false);

/// <summary>Huawei CBS / external billing bridge (implemented in Infrastructure).</summary>
public interface IBillingSystemIntegration
{
    Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default);
    Task<BillingProvisionResult> ReverseProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default);
    Task<decimal> GetOutstandingBalanceAsync(string msisdn, CancellationToken cancellationToken = default);
}

public interface IChargingSystemIntegration
{
    Task<BillingProvisionResult> TouchAsync(string correlationId, CancellationToken cancellationToken = default);
}

public interface ISmsGatewayIntegration
{
    Task<BillingProvisionResult> SendAsync(string phoneNumber, string body, CancellationToken cancellationToken = default);
}
