using Application.Common.Integrations;
using Application.Features.TelecomManager.Queries;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class VoucherValidationTests
{
    private sealed class StubGateway : IPaymentGatewayIntegration
    {
        public Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PaymentConfirmResult> ConfirmAsync(PaymentConfirmRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<VoucherValidationResult> ValidateVoucherAsync(VoucherValidationRequest request, CancellationToken cancellationToken = default)
        {
            if (request.VoucherCode == "VOUCH-EXPIRED")
            {
                return Task.FromResult(new VoucherValidationResult(false, "منتهية", IsExpired: true));
            }

            return Task.FromResult(new VoucherValidationResult(true, "صالحة", 5000m));
        }

        public Task<VoucherValidationResult> RedeemVoucherAsync(string voucherCode, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PaymentReverseResult> ReverseAsync(PaymentReverseRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentReverseResult(true, "ok"));

        public Task<WalletRefundResult> RefundToWalletAsync(WalletRefundRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WalletRefundResult(true, "ok", "WALLET-STUB"));
    }

    [Fact]
    public async Task ValidateVoucher_query_maps_gateway_result()
    {
        var handler = new ValidateVoucherHandler(new StubGateway());
        var result = await handler.Handle(new ValidateVoucherRequest { VoucherCode = "ANY" }, CancellationToken.None);
        Assert.True(result.Valid);
        Assert.Equal(5000m, result.FaceValue);
    }
}
