using Domain.Enums;

namespace Application.Common.Integrations;

public sealed record BillingProvisionRequest(
    string OperationId,
    string OperationNumber,
    string? Msisdn,
    TelecomOperationKind Kind);

public sealed record BillingProvisionResult(bool Success, string Message);

/// <summary>Huawei CBS / external billing bridge (implemented in Infrastructure).</summary>
public interface IBillingSystemIntegration
{
    Task<BillingProvisionResult> ProvisionAsync(BillingProvisionRequest request, CancellationToken cancellationToken = default);
}

public interface IChargingSystemIntegration
{
    Task<BillingProvisionResult> TouchAsync(string correlationId, CancellationToken cancellationToken = default);
}

public interface ISmsGatewayIntegration
{
    Task<BillingProvisionResult> SendAsync(string phoneNumber, string body, CancellationToken cancellationToken = default);
}
