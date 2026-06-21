using Application.Common.Integrations;
using Application.Common.Telecom;
using Domain.Enums;

namespace Infrastructure.TelecomIntegrations;

/// <summary>Sandbox cashier — accepts receipts prefixed with REC- and returns the expected deposit amount.</summary>
public sealed class CashierSystemMockIntegration : IPosCashierIntegration
{
    private const decimal DemoDefaultAmount = 50_000m;

    public async Task<CashierPaymentResultDto> FetchPaymentByReferenceAsync(
        string paymentReference,
        decimal expectedAmount,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(350, cancellationToken);

        var reference = (paymentReference ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(reference))
        {
            return new CashierPaymentResultDto(
                false,
                "رقم إيصال الكاشير مطلوب.",
                null,
                null,
                null);
        }

        if (TelecomDemoBaselines.IsDebtFullPaymentReceipt(reference))
        {
            var debtAmount = TelecomDemoBaselines.DebtFullPaymentAmountSyp;
            return new CashierPaymentResultDto(
                true,
                $"تم جلب وصل الدفع التجريبي — {debtAmount:N0} ل.س (مطابق لذمة مازن).",
                reference,
                debtAmount,
                PaymentChannel.Cash);
        }

        if (!reference.StartsWith("REC-", StringComparison.OrdinalIgnoreCase))
        {
            return new CashierPaymentResultDto(
                false,
                "إيصال الكاشير غير معروف — يجب أن يبدأ بـ REC- أو استخدم RCPT-2002 (Sandbox).",
                reference,
                null,
                null);
        }

        var amount = expectedAmount > 0 ? expectedAmount : DemoDefaultAmount;
        return new CashierPaymentResultDto(
            true,
            $"تم جلب الدفع من كاشير سيريتل (Sandbox) — {amount:N0} ل.س.",
            reference,
            amount,
            PaymentChannel.Cash);
    }
}
