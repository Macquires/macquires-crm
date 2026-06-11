namespace Application.Common.Integrations;

public interface IIntegrationOutbox
{
    Task EnqueueAsync(
        string eventType,
        string payloadJson,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
