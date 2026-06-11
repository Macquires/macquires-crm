using Domain.Common;
using Domain.Events;
using MediatR;

namespace Application.Common.Events;

public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public MediatRDomainEventDispatcher(IPublisher publisher) => _publisher = publisher;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            switch (domainEvent)
            {
                case SubscriberSuspendedEvent suspended:
                    await _publisher.Publish(
                        SubscriberSuspendedNotification.FromDomain(suspended),
                        cancellationToken);
                    break;
                case INotification notification:
                    await _publisher.Publish(notification, cancellationToken);
                    break;
            }
        }
    }

    public Task PublishNotificationAsync(INotification notification, CancellationToken cancellationToken = default) =>
        _publisher.Publish(notification, cancellationToken);
}
