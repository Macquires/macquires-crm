using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Unified payment ledger (PAY-) for recharge, vouchers, bill pay, and activation deposits.</summary>
public class TelecomPaymentTransaction : BaseEntity, IHasBranchId
{
    public string? BranchId { get; set; }
    public string Number { get; set; } = null!;

    public string? CorrelationId { get; set; }

    public PaymentTransactionType TransactionType { get; set; }

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Draft;

    public PaymentChannel PaymentChannel { get; set; }

    public PaymentServiceChannel ServiceChannel { get; set; } = PaymentServiceChannel.Showroom;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "SYP";

    public string CustomerId { get; set; } = null!;

    public string SubscriberProfileId { get; set; } = null!;

    public string? TelecomSubscriptionId { get; set; }

    public string? Msisdn { get; set; }

    public string? GatewayReference { get; set; }

    public string? GatewayTransactionId { get; set; }

    public string? ReceiptNumber { get; set; }

    public decimal? BalanceBefore { get; set; }

    public decimal? BalanceAfter { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }

    public string? VoucherCode { get; set; }

    public string? TelecomOperationRequestId { get; set; }

    public bool IsReversal { get; set; }

    public string? OriginalPaymentId { get; set; }

    public string? ReversalReasonCode { get; set; }

    public DateTime? ReversedAtUtc { get; set; }
}
