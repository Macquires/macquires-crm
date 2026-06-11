using System.Text.Json;
using Application.Common.Events;
using Application.Common.Integrations;
using Application.Common.Telecom;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.TelecomIntegrations.Outbox;

public sealed class TelecomProvisionedEventDispatcher : ITelecomProvisionedEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly IIntegrationOutbox _outbox;
    private readonly bool _useOutbox;

    public TelecomProvisionedEventDispatcher(
        IPublisher publisher,
        IIntegrationOutbox outbox,
        IConfiguration configuration)
    {
        _publisher = publisher;
        _outbox = outbox;
        _useOutbox = configuration.GetValue("Database:UseOutbox", true);
    }

    public async Task DispatchAsync(TelecomOperationProvisionedNotification notification, CancellationToken cancellationToken = default)
    {
        if (_useOutbox)
        {
            await _outbox.EnqueueAsync(
                nameof(TelecomOperationProvisionedNotification),
                JsonSerializer.Serialize(notification),
                notification.CorrelationId,
                cancellationToken);
            return;
        }

        await _publisher.Publish(notification, cancellationToken);
    }
}
