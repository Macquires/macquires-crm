namespace Domain.Common;

public class BaseEntity : IHasSequentialId, IHasIsDeleted, IHasAudit
{
    public string Id { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent eventItem) => _domainEvents.Add(eventItem);
    public void RemoveDomainEvent(IDomainEvent eventItem) => _domainEvents.Remove(eventItem);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public BaseEntity()
    {
        Id = Guid.CreateVersion7().ToString();
        IsDeleted = false;
    }
}
