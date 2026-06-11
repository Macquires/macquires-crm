using Domain.Enums;
using MediatR;

namespace Application.Common.Events;

/// <summary>MediatR bridge notification raised from <see cref="Domain.Events.SubscriberSuspendedEvent"/>.</summary>
public sealed record SubscriberSuspendedNotification(
    string SubscriberProfileId,
    string Msisdn,
    string CustomerId,
    SubscriberOperationalStatus Status) : INotification
{
    public static SubscriberSuspendedNotification FromDomain(Domain.Events.SubscriberSuspendedEvent e) =>
        new(e.SubscriberProfileId, e.Msisdn, e.CustomerId, e.Status);
}
