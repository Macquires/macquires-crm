namespace Infrastructure.TelecomIntegrations.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";
    public string Exchange { get; set; } = "nsuite.integrations";
    public string Queue { get; set; } = "telecom.provisioning";
    public string RoutingKey { get; set; } = "telecom.provisioning";
}
