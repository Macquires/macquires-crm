using Domain.Enums;

namespace Application.Common.Integrations;

public record PaymentCaptureRequest(
    string TelecomOperationRequestId,
    string OperationNumber,
    string? CorrelationId,
    decimal Amount,
    PaymentChannel Channel,
    string PaymentReference);

public record PaymentCaptureResult(bool Success, string Message, string? GatewayTransactionId);

public record PaymentConfirmRequest(
    string PaymentTransactionId,
    string PaymentNumber,
    decimal Amount,
    PaymentChannel Channel,
    string GatewayReference);

public record PaymentConfirmResult(bool Success, string Message, string? GatewayTransactionId);

public record PaymentReverseRequest(
    string PaymentTransactionId,
    string PaymentNumber,
    string GatewayReference,
    decimal Amount);

public record PaymentReverseResult(bool Success, string Message);

public record WalletRefundRequest(
    string TelecomOperationRequestId,
    string OperationNumber,
    string Msisdn,
    decimal Amount,
    string? CorrelationId);

public record WalletRefundResult(bool Success, string Message, string? GatewayTransactionId);

public record VoucherValidationRequest(string VoucherCode, decimal? ExpectedAmount = null);

public record VoucherValidationResult(
    bool Valid,
    string MessageAr,
    decimal? FaceValue = null,
    bool IsExpired = false,
    bool IsAlreadyUsed = false);

public interface IPaymentGatewayIntegration
{
    Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken = default);
    Task<PaymentConfirmResult> ConfirmAsync(PaymentConfirmRequest request, CancellationToken cancellationToken = default);
    Task<VoucherValidationResult> ValidateVoucherAsync(VoucherValidationRequest request, CancellationToken cancellationToken = default);
    Task<VoucherValidationResult> RedeemVoucherAsync(string voucherCode, CancellationToken cancellationToken = default);
    Task<PaymentReverseResult> ReverseAsync(PaymentReverseRequest request, CancellationToken cancellationToken = default);

    Task<WalletRefundResult> RefundToWalletAsync(WalletRefundRequest request, CancellationToken cancellationToken = default);
}
