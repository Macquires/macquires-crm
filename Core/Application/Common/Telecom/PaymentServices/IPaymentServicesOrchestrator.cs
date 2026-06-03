using Domain.Enums;

namespace Application.Common.Telecom.PaymentServices;

public sealed record PaymentDraftResult(
    string PaymentId,
    string Number,
    string? CorrelationId,
    PaymentTransactionStatus Status);

public sealed record PaymentConfirmOrchestrationResult(
    bool Success,
    PaymentTransactionStatus Status,
    string? ReceiptNumber,
    decimal? NewBalance,
    string? CorrelationId,
    string? MessageAr,
    string? PaymentNumber,
    string? Msisdn = null);

public interface IPaymentServicesOrchestrator
{
    Task<PaymentDraftResult> CreateRechargeDraftAsync(
        string customerId,
        string subscriptionId,
        decimal amount,
        PaymentChannel paymentChannel,
        PaymentServiceChannel serviceChannel,
        string? createdById,
        CancellationToken cancellationToken = default);

    Task<PaymentDraftResult> CreateVoucherRedeemDraftAsync(
        string customerId,
        string subscriptionId,
        string voucherCode,
        PaymentServiceChannel serviceChannel,
        string? createdById,
        CancellationToken cancellationToken = default);

    Task<PaymentConfirmOrchestrationResult> ConfirmAsync(
        string paymentId,
        string gatewayReference,
        string? confirmedById,
        CancellationToken cancellationToken = default);
}
