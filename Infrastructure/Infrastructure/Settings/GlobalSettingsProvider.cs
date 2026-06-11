using System.Globalization;
using Application.Common.Settings;
using Application.Common.Settings.Telecom;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Settings;

public class GlobalSettingsProvider : IGlobalSettingsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private readonly DataContext _context;
    private readonly IMemoryCache _cache;

    public GlobalSettingsProvider(DataContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _cache = memoryCache;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var map = await GetAllCachedAsync(cancellationToken);
        return map.TryGetValue(key, out var value) ? value : null;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default)
    {
        var raw = await GetValueAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue, int min = int.MinValue, int max = int.MaxValue, CancellationToken cancellationToken = default)
    {
        var raw = await GetValueAsync(key, cancellationToken);
        if (!int.TryParse(raw, out var value))
        {
            value = defaultValue;
        }

        return Math.Clamp(value, min, max);
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue, decimal min = decimal.MinValue, decimal max = decimal.MaxValue, CancellationToken cancellationToken = default)
    {
        var raw = await GetValueAsync(key, cancellationToken);
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            value = defaultValue;
        }

        return Math.Clamp(value, min, max);
    }

    public async Task<IReadOnlyList<string>> GetCsvListAsync(string key, CancellationToken cancellationToken = default)
    {
        var raw = await GetValueAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .ToList();
    }

    public Task<bool> GetCatalogBoolAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        var fallback = bool.TryParse(definition.DefaultValue, out var def) && def;
        return GetBoolAsync(definition.Key, fallback, cancellationToken);
    }

    public async Task<int> GetCatalogIntAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        var fallback = int.TryParse(definition.DefaultValue, out var def) ? def : 0;
        var min = definition.Min.HasValue ? (int)definition.Min.Value : int.MinValue;
        var max = definition.Max.HasValue ? (int)definition.Max.Value : int.MaxValue;
        return await GetIntAsync(definition.Key, fallback, min, max, cancellationToken);
    }

    public async Task<decimal> GetCatalogDecimalAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        var fallback = decimal.TryParse(definition.DefaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var def)
            ? def
            : 0m;
        var min = definition.Min ?? decimal.MinValue;
        var max = definition.Max ?? decimal.MaxValue;
        return await GetDecimalAsync(definition.Key, fallback, min, max, cancellationToken);
    }

    public async Task<string> GetCatalogStringAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        var raw = await GetValueAsync(definition.Key, cancellationToken);
        return string.IsNullOrWhiteSpace(raw) ? definition.DefaultValue : raw;
    }

    public async Task<IReadOnlyList<string>> GetCatalogCsvListAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        var list = await GetCsvListAsync(definition.Key, cancellationToken);
        if (list.Count == 0 && !string.IsNullOrWhiteSpace(definition.DefaultValue))
        {
            return definition.DefaultValue
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        return list;
    }

    private async Task<IReadOnlyDictionary<string, string>> GetAllCachedAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            "GlobalSettings:All",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var rows = await _context.GlobalSetting.AsNoTracking().ToListAsync(cancellationToken);
                return rows.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            }) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public void InvalidateCache() => _cache.Remove("GlobalSettings:All");
}
