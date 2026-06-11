using Domain.Common;
using Domain.Enums;

namespace Domain.Events;

public sealed record SubscriberSuspendedEvent(
    string SubscriberProfileId,
    string Msisdn,
    string CustomerId,
    SubscriberOperationalStatus Status) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
