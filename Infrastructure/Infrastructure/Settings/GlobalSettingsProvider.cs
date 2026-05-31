using Application.Common.Settings;
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
