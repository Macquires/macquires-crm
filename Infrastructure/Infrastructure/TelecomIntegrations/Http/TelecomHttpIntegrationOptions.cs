namespace Infrastructure.TelecomIntegrations.Http;

public sealed class TelecomHttpIntegrationOptions
{
    public const string SectionName = "TelecomIntegrations";

    public string Mode { get; set; } = "Mock";
    public string SimulatorBaseUrl { get; set; } = "http://localhost:5099";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
}
