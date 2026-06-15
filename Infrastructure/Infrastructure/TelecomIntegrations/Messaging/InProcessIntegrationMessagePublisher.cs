using System.Text.Json;
using Application.Common.Events;
using Application.Common.Integrations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations.Messaging;

/// <summary>Fallback publisher: dispatches directly via MediatR when RabbitMQ is disabled.</summary>
public sealed class InProcessIntegrationMessagePublisher : IIntegrationMessagePublisher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<InProcessIntegrationMessagePublisher> _logger;

    public InProcessIntegrationMessagePublisher(
        IPublisher publisher,
        ILogger<InProcessIntegrationMessagePublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishAsync(
        string eventType,
        string payloadJson,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(eventType, nameof(TelecomOperationProvisionedNotification), StringComparison.Ordinal))
        {
            var notification = JsonSerializer.Deserialize<TelecomOperationProvisionedNotification>(payloadJson);
            if (notification != null)
            {
                await _publisher.Publish(notification, cancellationToken);
                return;
            }
        }

        _logger.LogWarning("Unknown integration event type {EventType} (correlation {CorrelationId})", eventType, correlationId);
    }
}
