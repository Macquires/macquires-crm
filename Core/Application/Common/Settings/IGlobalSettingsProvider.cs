using Application.Common.Settings.Telecom;

namespace Application.Common.Settings;

public interface IGlobalSettingsProvider
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCsvListAsync(string key, CancellationToken cancellationToken = default);
    Task<int> GetIntAsync(string key, int defaultValue, int min = int.MinValue, int max = int.MaxValue, CancellationToken cancellationToken = default);
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue, decimal min = decimal.MinValue, decimal max = decimal.MaxValue, CancellationToken cancellationToken = default);

    Task<bool> GetCatalogBoolAsync(SettingDefinition definition, CancellationToken cancellationToken = default);
    Task<int> GetCatalogIntAsync(SettingDefinition definition, CancellationToken cancellationToken = default);
    Task<decimal> GetCatalogDecimalAsync(SettingDefinition definition, CancellationToken cancellationToken = default);
    Task<string> GetCatalogStringAsync(SettingDefinition definition, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCatalogCsvListAsync(SettingDefinition definition, CancellationToken cancellationToken = default);
}
