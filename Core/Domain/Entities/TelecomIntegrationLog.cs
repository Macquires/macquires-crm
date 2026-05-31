using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Centralized request/response log for external telecom integrations (HLR, CBS, SMS).</summary>
public class TelecomIntegrationLog : BaseEntity
{
    public string? Msisdn { get; set; }
    public TelecomIntegrationSystem IntegrationSystem { get; set; }
    public string OperationName { get; set; } = null!;
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResponseStatusCode { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
