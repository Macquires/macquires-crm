using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class TakeOverObligationSettlementMockIntegration : ITakeOverObligationSettlementIntegration
{
    public Task<TakeOverObligationSettlementResult> SettleAsync(
        TakeOverObligationSettlementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.ObligationStatus, "PendingSettlement", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.PaymentReference))
        {
            return Task.FromResult(new TakeOverObligationSettlementResult(
                false,
                "مرجع تسوية الالتزامات مطلوب قبل نقل الملكية.",
                null));
        }

        var reference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? "OBL-CLEAR"
            : request.PaymentReference.Trim();

        return Task.FromResult(new TakeOverObligationSettlementResult(true, string.Empty, reference));
    }
}
