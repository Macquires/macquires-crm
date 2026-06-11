using Domain.Common;

namespace Domain.Entities;

public sealed class IdempotencyRecord : BaseEntity
{
    public string Key { get; set; } = null!;
    public string Scope { get; set; } = null!;
    public string? RequestHash { get; set; }
    public string? ResponsePayload { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
