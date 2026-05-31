using Domain.Common;

namespace Domain.Entities;

/// <summary>Administrative and security audit trail for user actions.</summary>
public class UserAuditLog : BaseEntity
{
    public string? UserId { get; set; }
    public string ActorUserId { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? SummaryAr { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? IpAddress { get; set; }
}
