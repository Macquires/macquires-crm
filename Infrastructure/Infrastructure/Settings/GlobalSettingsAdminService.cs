using Application.Common.Settings;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Settings;

public class GlobalSettingsAdminService : IGlobalSettingsAdminService
{
    private const string CacheKey = "GlobalSettings:All";
    private readonly DataContext _context;
    private readonly IMemoryCache _cache;

    public GlobalSettingsAdminService(DataContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<GlobalSettingsSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var map = await LoadMapAsync(cancellationToken);
        return MapFromDictionary(map);
    }

    public async Task SaveSnapshotAsync(GlobalSettingsSnapshotDto snapshot, string? updatedById, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entries = new Dictionary<string, (string Value, string Category)>(StringComparer.OrdinalIgnoreCase)
        {
            [GlobalSettingKeys.MaintenanceMode] = (snapshot.MaintenanceMode.ToString().ToLowerInvariant(), "Maintenance"),
            [GlobalSettingKeys.MaintenanceMessageAr] = (snapshot.MaintenanceMessageAr ?? "", "Maintenance"),
            [GlobalSettingKeys.MaintenanceMessageEn] = (snapshot.MaintenanceMessageEn ?? "", "Maintenance"),
            [GlobalSettingKeys.MaintenanceBypassIPs] = (snapshot.MaintenanceBypassIPs ?? "", "Maintenance"),
            [GlobalSettingKeys.IsStrictPersonaMode] = (snapshot.IsStrictPersonaMode.ToString().ToLowerInvariant(), "Security"),
            [GlobalSettingKeys.JwtAccessTokenMinutes] = (Math.Clamp(snapshot.JwtAccessTokenMinutes, 5, 1440).ToString(), "Security"),
            [GlobalSettingKeys.IntegrationHuaweiEnabled] = (snapshot.IntegrationHuaweiEnabled.ToString().ToLowerInvariant(), "Integration"),
            [GlobalSettingKeys.IntegrationHlrEnabled] = (snapshot.IntegrationHlrEnabled.ToString().ToLowerInvariant(), "Integration"),
            [GlobalSettingKeys.IntegrationSmsEnabled] = (snapshot.IntegrationSmsEnabled.ToString().ToLowerInvariant(), "Integration"),
        };

        foreach (var (key, (value, category)) in entries)
        {
            var row = await _context.GlobalSetting.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
            if (row == null)
            {
                await _context.GlobalSetting.AddAsync(new GlobalSetting
                {
                    Key = key,
                    Value = value,
                    Category = category,
                    UpdatedAtUtc = now,
                    UpdatedById = updatedById,
                }, cancellationToken);
            }
            else
            {
                row.Value = value;
                row.Category = category;
                row.UpdatedAtUtc = now;
                row.UpdatedById = updatedById;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadMapAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.GlobalSetting.AsNoTracking().ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static GlobalSettingsSnapshotDto MapFromDictionary(IReadOnlyDictionary<string, string> map)
    {
        static bool Bool(string key, bool def, IReadOnlyDictionary<string, string> m) =>
            m.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : def;

        static string Str(string key, string def, IReadOnlyDictionary<string, string> m) =>
            m.TryGetValue(key, out var v) ? v : def;

        static int Int(string key, int def, IReadOnlyDictionary<string, string> m) =>
            m.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : def;

        return new GlobalSettingsSnapshotDto
        {
            MaintenanceMode = Bool(GlobalSettingKeys.MaintenanceMode, false, map),
            MaintenanceMessageAr = Str(GlobalSettingKeys.MaintenanceMessageAr, "", map),
            MaintenanceMessageEn = Str(GlobalSettingKeys.MaintenanceMessageEn, "", map),
            MaintenanceBypassIPs = Str(GlobalSettingKeys.MaintenanceBypassIPs, "127.0.0.1", map),
            IsStrictPersonaMode = Bool(GlobalSettingKeys.IsStrictPersonaMode, true, map),
            JwtAccessTokenMinutes = Int(GlobalSettingKeys.JwtAccessTokenMinutes, 60, map),
            IntegrationHuaweiEnabled = Bool(GlobalSettingKeys.IntegrationHuaweiEnabled, true, map),
            IntegrationHlrEnabled = Bool(GlobalSettingKeys.IntegrationHlrEnabled, true, map),
            IntegrationSmsEnabled = Bool(GlobalSettingKeys.IntegrationSmsEnabled, true, map),
        };
    }
}
