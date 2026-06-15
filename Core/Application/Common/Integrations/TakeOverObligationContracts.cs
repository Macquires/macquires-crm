namespace Application.Common.Integrations;

public sealed record TakeOverObligationSettlementRequest(
    string OperationId,
    string OperationNumber,
    string Msisdn,
    string ObligationStatus,
    string? PaymentReference,
    decimal? SettlementAmount,
    string? CorrelationId = null,
    string? BranchId = null);

public sealed record TakeOverObligationSettlementResult(
    bool Success,
    string Message,
    string? SettlementReference = null);

/// <summary>Settles prior-owner obligations before take-over completion.</summary>
public interface ITakeOverObligationSettlementIntegration
{
    Task<TakeOverObligationSettlementResult> SettleAsync(
        TakeOverObligationSettlementRequest request,
        CancellationToken cancellationToken = default);
}
