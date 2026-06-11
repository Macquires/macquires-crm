namespace Application.Common.Telecom;

public interface ITelecomInventoryRulesProvider
{
    Task<TimeSpan> GetMsisdnReservationDurationAsync(CancellationToken cancellationToken = default);
    Task<int> GetMsisdnQuarantineDaysAsync(CancellationToken cancellationToken = default);
    Task<int> GetSimQuarantineDaysAsync(CancellationToken cancellationToken = default);
    Task<int> GetDormantLineScanIntervalHoursAsync(CancellationToken cancellationToken = default);
    Task<int> GetDormantLineInactivityDaysAsync(CancellationToken cancellationToken = default);
}

public sealed class TelecomInventoryRulesProvider : ITelecomInventoryRulesProvider
{
    private readonly Application.Common.Settings.IGlobalSettingsProvider _settings;

    public TelecomInventoryRulesProvider(Application.Common.Settings.IGlobalSettingsProvider settings)
    {
        _settings = settings;
    }

    public async Task<TimeSpan> GetMsisdnReservationDurationAsync(CancellationToken cancellationToken = default)
    {
        var minutes = await _settings.GetCatalogIntAsync(
            Application.Common.Settings.Telecom.TelecomBusinessRulesCatalog.FindByKey(
                Application.Common.Settings.GlobalSettingKeys.TelecomInventoryMsisdnReservationMinutes)!,
            cancellationToken);
        if (minutes > 0)
        {
            return TimeSpan.FromMinutes(minutes);
        }

        var hours = await _settings.GetCatalogIntAsync(
            Application.Common.Settings.Telecom.TelecomBusinessRulesCatalog.FindByKey(
                Application.Common.Settings.GlobalSettingKeys.TelecomInventoryMsisdnReservationHours)!,
            cancellationToken);
        return TimeSpan.FromHours(Math.Max(1, hours));
    }

    public Task<int> GetMsisdnQuarantineDaysAsync(CancellationToken cancellationToken = default) =>
        _settings.GetCatalogIntAsync(
            Application.Common.Settings.Telecom.TelecomBusinessRulesCatalog.FindByKey(
                Application.Common.Settings.GlobalSettingKeys.TelecomInventoryMsisdnQuarantineDays)!,
            cancellationToken);

    public Task<int> GetSimQuarantineDaysAsync(CancellationToken cancellationToken = default) =>
        _settings.GetCatalogIntAsync(
            Application.Common.Settings.Telecom.TelecomBusinessRulesCatalog.FindByKey(
                Application.Common.Settings.GlobalSettingKeys.TelecomInventorySimQuarantineDays)!,
            cancellationToken);

    public Task<int> GetDormantLineScanIntervalHoursAsync(CancellationToken cancellationToken = default) =>
        _settings.GetCatalogIntAsync(
            Application.Common.Settings.Telecom.TelecomBusinessRulesCatalog.FindByKey(
                Application.Common.Settings.GlobalSettingKeys.TelecomInventoryDormantLineScanIntervalHours)!,
            cancellationToken);

    public Task<int> GetDormantLineInactivityDaysAsync(CancellationToken cancellationToken = default) =>
        _settings.GetCatalogIntAsync(
            Application.Common.Settings.Telecom.TelecomBusinessRulesCatalog.FindByKey(
                Application.Common.Settings.GlobalSettingKeys.TelecomInventoryDormantLineInactivityDays)!,
            cancellationToken);
}
