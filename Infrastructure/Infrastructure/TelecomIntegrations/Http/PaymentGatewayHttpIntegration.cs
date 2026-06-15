using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations.Http;

public sealed class PaymentGatewayHttpIntegration : IPaymentGatewayIntegration
{
    private readonly PaymentGatewayMockIntegration _fallback;

    public PaymentGatewayHttpIntegration(PaymentGatewayMockIntegration fallback) => _fallback = fallback;

    public Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken = default) =>
        _fallback.CaptureAsync(request, cancellationToken);

    public Task<PaymentConfirmResult> ConfirmAsync(PaymentConfirmRequest request, CancellationToken cancellationToken = default) =>
        _fallback.ConfirmAsync(request, cancellationToken);

    public Task<VoucherValidationResult> ValidateVoucherAsync(
        VoucherValidationRequest request,
        CancellationToken cancellationToken = default) =>
        _fallback.ValidateVoucherAsync(request, cancellationToken);

    public Task<PaymentReverseResult> ReverseAsync(PaymentReverseRequest request, CancellationToken cancellationToken = default) =>
        _fallback.ReverseAsync(request, cancellationToken);

    public Task<WalletRefundResult> RefundToWalletAsync(WalletRefundRequest request, CancellationToken cancellationToken = default) =>
        _fallback.RefundToWalletAsync(request, cancellationToken);

    public Task<VoucherValidationResult> RedeemVoucherAsync(string voucherCode, CancellationToken cancellationToken = default) =>
        _fallback.RedeemVoucherAsync(voucherCode, cancellationToken);
}
