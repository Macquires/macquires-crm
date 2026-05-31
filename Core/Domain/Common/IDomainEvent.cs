namespace Domain.Common;

/// <summary>Marker for domain events dispatched after successful unit of work.</summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
