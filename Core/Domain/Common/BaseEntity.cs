namespace Domain.Common;

public class BaseEntity : IHasSequentialId, IHasIsDeleted, IHasAudit
{
    public string Id { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }

    public BaseEntity()
    {
        // .NET 9.0 UUID Version 7 (RFC 9562): time-ordered, lock-free, GC-friendly.
        // Replaces legacy COMB GUID that used lock(_lock) and caused thread contention.
        Id = Guid.CreateVersion7().ToString();
        IsDeleted = false;
    }
}

