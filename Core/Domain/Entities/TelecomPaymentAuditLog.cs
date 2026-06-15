using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class TelecomPaymentAuditLog : BaseEntity, IHasBranchId
{
    public string? BranchId { get; set; }
    public string TelecomPaymentTransactionId { get; set; } = null!;
    public TelecomPaymentTransaction? TelecomPaymentTransaction { get; set; }

    public string Action { get; set; } = null!;

    public PaymentTransactionStatus? FromStatus { get; set; }

    public PaymentTransactionStatus? ToStatus { get; set; }

    public decimal? BalanceBefore { get; set; }

    public decimal? BalanceAfter { get; set; }

    public string? GatewayReference { get; set; }

    public string? ActorUserId { get; set; }

    public string? ReasonCode { get; set; }

    public string? Note { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
