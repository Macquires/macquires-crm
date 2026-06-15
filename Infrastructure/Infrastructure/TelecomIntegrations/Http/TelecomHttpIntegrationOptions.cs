namespace Infrastructure.TelecomIntegrations.Http;

public sealed class TelecomHttpIntegrationOptions
{
    public const string SectionName = "TelecomIntegrations";

    /// <summary>Legacy global mode (Mock|Http) — used when per-adapter Mode is unset.</summary>
    public string Mode { get; set; } = "Mock";

    public string SimulatorBaseUrl { get; set; } = "http://localhost:5099";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;

    public AdapterModeOptions Mnp { get; set; } = new();
    public AdapterModeOptions IntelligentNetwork { get; set; } = new();
    public AdapterModeOptions Cashier { get; set; } = new();
    public AdapterModeOptions PaymentGateway { get; set; } = new();
    public AdapterModeOptions DeviceInventory { get; set; } = new();
    public AdapterModeOptions ESim { get; set; } = new();
    public AdapterModeOptions Sms { get; set; } = new();
}

public sealed class AdapterModeOptions
{
    public string Mode { get; set; } = "Mock";
}
