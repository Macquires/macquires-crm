namespace Application.Common.Integrations;

/// <summary>Publishes integration events from the outbox to a durable bus (RabbitMQ) or in-process fallback.</summary>
public interface IIntegrationMessagePublisher
{
    Task PublishAsync(
        string eventType,
        string payloadJson,
        string? correlationId,
        CancellationToken cancellationToken = default);
}
