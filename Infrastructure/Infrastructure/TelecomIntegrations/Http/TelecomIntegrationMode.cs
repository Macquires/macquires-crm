namespace Infrastructure.TelecomIntegrations.Http;

public static class TelecomIntegrationMode
{
    public static bool IsHttp(TelecomHttpIntegrationOptions options) =>
        string.Equals(options.Mode, "Http", StringComparison.OrdinalIgnoreCase);

    public static bool IsHttp(TelecomHttpIntegrationOptions options, AdapterModeOptions adapter) =>
        string.Equals(adapter.Mode, "Http", StringComparison.OrdinalIgnoreCase)
        || (string.IsNullOrWhiteSpace(adapter.Mode)
            || string.Equals(adapter.Mode, "Default", StringComparison.OrdinalIgnoreCase))
            && IsHttp(options);
}
