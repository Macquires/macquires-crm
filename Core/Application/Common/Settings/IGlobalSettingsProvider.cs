namespace Application.Common.Settings;

public interface IGlobalSettingsProvider
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCsvListAsync(string key, CancellationToken cancellationToken = default);
    Task<int> GetIntAsync(string key, int defaultValue, int min = int.MinValue, int max = int.MaxValue, CancellationToken cancellationToken = default);
}
