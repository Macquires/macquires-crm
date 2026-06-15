namespace Application.Common.Integrations;

public sealed record VasBillingChargeRequest(
    string OperationId,
    string Msisdn,
    string ServiceCode,
    decimal Amount,
    string? CorrelationId = null,
    string? BranchId = null);

public sealed record VasBillingChargeResult(
    bool Success,
    string Message,
    string? ChargeReference = null);

/// <summary>Charges recurring/one-off VAS fees via CBS when enabled.</summary>
public interface IVasBillingIntegration
{
    Task<VasBillingChargeResult> ChargeAsync(
        VasBillingChargeRequest request,
        CancellationToken cancellationToken = default);
}
