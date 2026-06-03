using System.Collections.Concurrent;
using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class PaymentGatewayMockIntegration : IPaymentGatewayIntegration
{
    private static readonly ConcurrentDictionary<string, decimal> DemoVouchers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VOUCH-DEMO-5000"] = 5000m,
        ["VOUCH-DEMO-15000"] = 15000m,
        ["VOUCH-DEMO-25000"] = 25000m,
    };

    private static readonly HashSet<string> UsedVouchers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> ExpiredVouchers = new(StringComparer.OrdinalIgnoreCase)
    {
        "VOUCH-EXPIRED",
        "VOUCH-DEMO-EXPIRED",
    };

    public Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentReference))
        {
            return Task.FromResult(new PaymentCaptureResult(false, "رقم مرجع الدفع مطلوب.", null));
        }

        if (request.Amount <= 0)
        {
            return Task.FromResult(new PaymentCaptureResult(false, "مبلغ الدفع يجب أن يكون أكبر من صفر.", null));
        }

        var txnId = $"PAY-MOCK-{request.PaymentReference.Trim()}";
        return Task.FromResult(new PaymentCaptureResult(true, "تم تسجيل الدفع (Mock).", txnId));
    }

    public Task<PaymentConfirmResult> ConfirmAsync(PaymentConfirmRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.GatewayReference))
        {
            return Task.FromResult(new PaymentConfirmResult(false, "رقم مرجع الدفع مطلوب.", null));
        }

        if (request.Amount <= 0)
        {
            return Task.FromResult(new PaymentConfirmResult(false, "مبلغ الدفع يجب أن يكون أكبر من صفر.", null));
        }

        var txnId = $"GW-CONFIRM-{request.GatewayReference.Trim()}";
        return Task.FromResult(new PaymentConfirmResult(true, "تم تأكيد الدفع عبر البوابة (Mock).", txnId));
    }

    public Task<VoucherValidationResult> ValidateVoucherAsync(
        VoucherValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = (request.VoucherCode ?? "").Trim();
        if (string.IsNullOrEmpty(code))
        {
            return Task.FromResult(new VoucherValidationResult(false, "رمز القسيمة مطلوب."));
        }

        if (ExpiredVouchers.Contains(code))
        {
            return Task.FromResult(new VoucherValidationResult(
                false,
                "VAL-12-02: القسيمة منتهية الصلاحية.",
                IsExpired: true));
        }

        if (string.Equals(code, "VOUCH-USED", StringComparison.OrdinalIgnoreCase) || UsedVouchers.Contains(code))
        {
            return Task.FromResult(new VoucherValidationResult(
                false,
                "VAL-12-02: القسيمة مستخدمة مسبقاً.",
                IsAlreadyUsed: true));
        }

        if (!DemoVouchers.TryGetValue(code, out var faceValue))
        {
            return Task.FromResult(new VoucherValidationResult(false, "VAL-12-02: رمز القسيمة غير معروف أو غير صالح."));
        }

        if (request.ExpectedAmount.HasValue && request.ExpectedAmount.Value != faceValue)
        {
            return Task.FromResult(new VoucherValidationResult(
                false,
                $"قيمة القسيمة {faceValue:N0} ل.س لا تطابق المبلغ المطلوب."));
        }

        return Task.FromResult(new VoucherValidationResult(
            true,
            $"القسيمة صالحة — قيمة الشحن {faceValue:N0} ل.س.",
            faceValue));
    }

    public Task<PaymentReverseResult> ReverseAsync(PaymentReverseRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.GatewayReference))
        {
            return Task.FromResult(new PaymentReverseResult(false, "مرجع الدفع مطلوب للعكس."));
        }

        return Task.FromResult(new PaymentReverseResult(true, "تم عكس الدفع عبر البوابة (Mock)."));
    }

    public Task<WalletRefundResult> RefundToWalletAsync(WalletRefundRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            return Task.FromResult(new WalletRefundResult(false, "مبلغ الاسترداد يجب أن يكون أكبر من صفر.", null));
        }

        if (string.IsNullOrWhiteSpace(request.Msisdn))
        {
            return Task.FromResult(new WalletRefundResult(false, "رقم الخط مطلوب لإيداع المحفظة.", null));
        }

        var txnId = $"WALLET-RFD-{request.OperationNumber.Trim()}";
        return Task.FromResult(new WalletRefundResult(true, "تم إيداع المحفظة (Mock).", txnId));
    }

    public Task<VoucherValidationResult> RedeemVoucherAsync(string voucherCode, CancellationToken cancellationToken = default)
    {
        var validation = ValidateVoucherAsync(new VoucherValidationRequest(voucherCode), cancellationToken);
        return validation.ContinueWith(
            t =>
            {
                if (!t.Result.Valid)
                {
                    return t.Result;
                }

                var code = voucherCode.Trim();
                UsedVouchers.Add(code);
                return new VoucherValidationResult(true, "تم استهلاك القسيمة.", t.Result.FaceValue);
            },
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
