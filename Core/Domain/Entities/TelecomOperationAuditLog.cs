using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Audit trail for <see cref="TelecomOperationRequest"/> status transitions.</summary>
public class TelecomOperationAuditLog : BaseEntity
{
    public string TelecomOperationRequestId { get; set; } = null!;
    public TelecomOperationRequest? TelecomOperationRequest { get; set; }

    public TelecomOperationStatus FromStatus { get; set; }
    public TelecomOperationStatus ToStatus { get; set; }
    public string? ActorUserId { get; set; }
    public string? Note { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public ActivationChannel? ActivationChannel { get; set; }

    public string? BranchId { get; set; }

    public string? DealerCode { get; set; }

    public string? OverrideReasonCode { get; set; }

    public string? CorrelationId { get; set; }

    /// <summary>JSON snapshot of field changes (before/after) for forensic audit.</summary>
    public string? FieldChangesJson { get; set; }
}
