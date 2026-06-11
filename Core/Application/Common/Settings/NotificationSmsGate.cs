namespace Application.Common.Settings;

/// <summary>Central gate for customer-facing SMS notifications (separate from integration circuit breaker).</summary>
public static class NotificationSmsGate
{
    public static async Task<bool> IsCustomerOpsEnabledAsync(
        IGlobalSettingsProvider settings,
        CancellationToken cancellationToken = default) =>
        await settings.GetBoolAsync(GlobalSettingKeys.NotificationSmsCustomerOpsEnabled, true, cancellationToken);

    public static async Task<bool> IsWelcomeEnabledAsync(
        IGlobalSettingsProvider settings,
        CancellationToken cancellationToken = default) =>
        await settings.GetBoolAsync(GlobalSettingKeys.NotificationSmsWelcomeEnabled, true, cancellationToken);

    public static async Task<string> ResolveWelcomeBodyAsync(
        IGlobalSettingsProvider settings,
        string msisdn,
        CancellationToken cancellationToken = default)
    {
        var template = await settings.GetValueAsync(GlobalSettingKeys.NotificationSmsWelcomeTemplateAr, cancellationToken);
        if (string.IsNullOrWhiteSpace(template))
        {
            template = "مرحباً بك في سيريتل. تم تفعيل خطك {Msisdn} بنجاح.";
        }

        return template.Replace("{Msisdn}", msisdn, StringComparison.OrdinalIgnoreCase);
    }
}
