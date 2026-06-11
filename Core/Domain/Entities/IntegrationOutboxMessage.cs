using Domain.Common;

namespace Domain.Entities;

public sealed class IntegrationOutboxMessage : BaseEntity
{
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
