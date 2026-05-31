namespace Application.Common.Audit;

public interface IUserAuditReadService
{
    Task<UserAuditLogQueryResult> QueryAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default);

    Task<int> CountAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default);
}

public sealed class UserAuditLogQuery
{
    /// <summary>Subscriber search: name, MSISDN, phone, national id, or customer GUID.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>When set, returns audit rows for this CRM customer only.</summary>
    public string? CustomerId { get; init; }
    public string? UserId { get; init; }
    public string? ActorUserId { get; init; }
    public string? ActionType { get; init; }
    public IReadOnlyList<string>? ActionTypes { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 100;
}

public sealed class UserAuditLogQueryResult
{
    public IReadOnlyList<UserAuditLogListItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed class UserAuditLogListItemDto
{
    public string Id { get; init; } = null!;
    public string? UserId { get; init; }
    public string ActorUserId { get; init; } = null!;
    public string ActionType { get; init; } = null!;
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? SummaryAr { get; init; }
    public string? PayloadJson { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string? IpAddress { get; init; }
    public string? ActorDisplayName { get; init; }
    public string? TargetDisplayName { get; init; }
}
