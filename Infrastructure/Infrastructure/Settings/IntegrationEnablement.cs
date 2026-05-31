using Application.Common.Integrations;
using Application.Common.Settings;

namespace Infrastructure.Settings;

/// <summary>Reads integration toggles from global settings (runtime on/off).</summary>
public class IntegrationEnablement
{
    private readonly IGlobalSettingsProvider _settings;

    public IntegrationEnablement(IGlobalSettingsProvider settings) => _settings = settings;

    public Task<bool> IsHuaweiEnabledAsync(CancellationToken ct = default) =>
        _settings.GetBoolAsync(GlobalSettingKeys.IntegrationHuaweiEnabled, true, ct);

    public Task<bool> IsHlrEnabledAsync(CancellationToken ct = default) =>
        _settings.GetBoolAsync(GlobalSettingKeys.IntegrationHlrEnabled, true, ct);

    public Task<bool> IsSmsEnabledAsync(CancellationToken ct = default) =>
        _settings.GetBoolAsync(GlobalSettingKeys.IntegrationSmsEnabled, true, ct);

    public static BillingProvisionResult DisabledBilling(string system) =>
        new(false, $"Integration '{system}' is disabled in Global Settings.");
}
