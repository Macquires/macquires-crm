namespace Application.Common.Audit;

public interface IUserAuditService
{
    Task LogAsync(UserAuditLogRequest entry, CancellationToken cancellationToken = default);
}

public sealed class UserAuditLogRequest
{
    public required string ActorUserId { get; init; }
    public required string ActionType { get; init; }
    public string? UserId { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? SummaryAr { get; init; }
    public object? Payload { get; init; }
    public string? IpAddress { get; init; }
}
