using Domain.Common;
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
            if (domainEvent is INotification notification)
            {
                await _publisher.Publish(notification, cancellationToken);
            }
        }
    }

    public Task PublishNotificationAsync(INotification notification, CancellationToken cancellationToken = default) =>
        _publisher.Publish(notification, cancellationToken);
}
