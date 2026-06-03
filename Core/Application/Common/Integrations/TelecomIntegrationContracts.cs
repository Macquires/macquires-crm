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
    TelecomBillingProvisionPhase Phase = TelecomBillingProvisionPhase.Provision,
    string? PriorIccid = null,
    string? PriorMsisdn = null);

public sealed record BillingProvisionResult(
    bool Success,
    string Message,
    bool QueuedForSync = false,
    bool IdempotentReplay = false);

/// <summary>Huawei CBS / external billing bridge (implemented in Infrastructure).</summary>
public sealed record BillingRechargeRequest(
    string PaymentTransactionId,
    string PaymentNumber,
    string Msisdn,
    decimal Amount,
    string? CorrelationId);

public sealed record BillingRechargeResult(
    bool Success,
    string Message,
    decimal? NewBalance = null);

public sealed record BillingReverseRechargeRequest(
    string PaymentTransactionId,
    string PaymentNumber,
    string Msisdn,
    decimal Amount,
    string? CorrelationId);

public interface IBillingSystemIntegration
{
    Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default);
    Task<BillingProvisionResult> ReverseProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default);
    Task<BillingRechargeResult> RechargeAsync(BillingRechargeRequest request, CancellationToken cancellationToken = default);
    Task<BillingRechargeResult> ReverseRechargeAsync(BillingReverseRechargeRequest request, CancellationToken cancellationToken = default);
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
