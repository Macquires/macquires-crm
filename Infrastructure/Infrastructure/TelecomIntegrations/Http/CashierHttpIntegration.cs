using Application.Common.Integrations;
using Domain.Enums;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class CashierHttpIntegration : IPosCashierIntegration
{
    private readonly CashierSystemMockIntegration _fallback;

    public CashierHttpIntegration(CashierSystemMockIntegration fallback) => _fallback = fallback;

    public Task<CashierPaymentResultDto> FetchPaymentByReferenceAsync(
        string paymentReference,
        decimal expectedAmount,
        CancellationToken cancellationToken = default) =>
        _fallback.FetchPaymentByReferenceAsync(paymentReference, expectedAmount, cancellationToken);
}
