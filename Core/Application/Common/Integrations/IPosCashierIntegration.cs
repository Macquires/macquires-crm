using Domain.Enums;

namespace Application.Common.Integrations;

public record CashierPaymentResultDto(
    bool Success,
    string MessageAr,
    string? PaymentReference,
    decimal? AmountPaid,
    PaymentChannel? PaymentChannel);

/// <summary>Syriatel POS / cashier receipt lookup (production HTTP adapter replaces mock).</summary>
public interface IPosCashierIntegration
{
    Task<CashierPaymentResultDto> FetchPaymentByReferenceAsync(
        string paymentReference,
        decimal expectedAmount,
        CancellationToken cancellationToken = default);
}
