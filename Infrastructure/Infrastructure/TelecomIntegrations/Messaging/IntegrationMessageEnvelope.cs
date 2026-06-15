namespace Infrastructure.TelecomIntegrations.Messaging;

public sealed class IntegrationMessageEnvelope
{
    public string EventType { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public string? CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
