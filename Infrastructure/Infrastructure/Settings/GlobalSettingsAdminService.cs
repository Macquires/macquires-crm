using Application.Common.Settings;

using Application.Common.Settings.Telecom;

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

        var existing = await GetSnapshotAsync(cancellationToken);

        var merged = MergeSnapshots(existing, snapshot);



        var now = DateTime.UtcNow;

        var entries = BuildCoreEntries(merged);

        foreach (var pair in TelecomBusinessRulesMapper.ToEntries(merged.Telecom!))

        {

            entries[pair.Key] = pair.Value;

        }



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



    /// <summary>

    /// دمج ذكي: إذا Telecom=null في الطلب الوارد، نحافظ على قيم DB الحالية

    /// حتى لا يمسح حفظ تبويب security/integrations إعدادات telecom بالخطأ.

    /// </summary>

    internal static GlobalSettingsSnapshotDto MergeSnapshots(GlobalSettingsSnapshotDto existing, GlobalSettingsSnapshotDto incoming)

    {

        return new GlobalSettingsSnapshotDto

        {

            MaintenanceMode = incoming.MaintenanceMode,

            MaintenanceMessageAr = incoming.MaintenanceMessageAr,

            MaintenanceMessageEn = incoming.MaintenanceMessageEn,

            MaintenanceBypassIPs = incoming.MaintenanceBypassIPs,

            IsStrictPersonaMode = incoming.IsStrictPersonaMode,

            IsDemoVersion = incoming.IsDemoVersion,

            JwtAccessTokenMinutes = incoming.JwtAccessTokenMinutes,

            IntegrationHuaweiEnabled = incoming.IntegrationHuaweiEnabled,

            IntegrationHlrEnabled = incoming.IntegrationHlrEnabled,

            IntegrationSmsEnabled = incoming.IntegrationSmsEnabled,

            IntegrationInEnabled = incoming.IntegrationInEnabled,

            IntegrationInGatewayUrl = incoming.IntegrationInGatewayUrl,

            IntegrationInTimeoutMilliseconds = incoming.IntegrationInTimeoutMilliseconds,

            IntegrationInSimulateRealTimeDeduction = incoming.IntegrationInSimulateRealTimeDeduction,

            NotificationSmsCustomerOpsEnabled = incoming.NotificationSmsCustomerOpsEnabled,

            NotificationSmsWelcomeEnabled = incoming.NotificationSmsWelcomeEnabled,

            NotificationSmsWelcomeTemplateAr = incoming.NotificationSmsWelcomeTemplateAr,

            NotificationSmsWelcomeTemplateEn = incoming.NotificationSmsWelcomeTemplateEn,

            Telecom = incoming.Telecom ?? existing.Telecom ?? TelecomBusinessRulesMapper.CreateWithCatalogDefaults(),

        };

    }



    private static Dictionary<string, (string Value, string Category)> BuildCoreEntries(GlobalSettingsSnapshotDto snapshot)

    {

        return new Dictionary<string, (string Value, string Category)>(StringComparer.OrdinalIgnoreCase)

        {

            [GlobalSettingKeys.MaintenanceMode] = (snapshot.MaintenanceMode.ToString().ToLowerInvariant(), "Maintenance"),

            [GlobalSettingKeys.MaintenanceMessageAr] = (snapshot.MaintenanceMessageAr ?? "", "Maintenance"),

            [GlobalSettingKeys.MaintenanceMessageEn] = (snapshot.MaintenanceMessageEn ?? "", "Maintenance"),

            [GlobalSettingKeys.MaintenanceBypassIPs] = (snapshot.MaintenanceBypassIPs ?? "", "Maintenance"),

            [GlobalSettingKeys.IsStrictPersonaMode] = (snapshot.IsStrictPersonaMode.ToString().ToLowerInvariant(), "Security"),

            [GlobalSettingKeys.IsDemoVersion] = (snapshot.IsDemoVersion.ToString().ToLowerInvariant(), "System"),

            [GlobalSettingKeys.JwtAccessTokenMinutes] = (Math.Clamp(snapshot.JwtAccessTokenMinutes, 5, 1440).ToString(), "Security"),

            [GlobalSettingKeys.IntegrationHuaweiEnabled] = (snapshot.IntegrationHuaweiEnabled.ToString().ToLowerInvariant(), "Integration"),

            [GlobalSettingKeys.IntegrationHlrEnabled] = (snapshot.IntegrationHlrEnabled.ToString().ToLowerInvariant(), "Integration"),

            [GlobalSettingKeys.IntegrationSmsEnabled] = (snapshot.IntegrationSmsEnabled.ToString().ToLowerInvariant(), "Integration"),

            [GlobalSettingKeys.IntegrationInEnabled] = (snapshot.IntegrationInEnabled.ToString().ToLowerInvariant(), "Integration"),

            [GlobalSettingKeys.IntegrationInGatewayUrl] = (snapshot.IntegrationInGatewayUrl ?? "https://in-gateway.syriatel.local/api/v1", "Integration"),

            [GlobalSettingKeys.IntegrationInTimeoutMilliseconds] = (Math.Clamp(snapshot.IntegrationInTimeoutMilliseconds, 500, 60000).ToString(), "Integration"),

            [GlobalSettingKeys.IntegrationInSimulateRealTimeDeduction] = (snapshot.IntegrationInSimulateRealTimeDeduction.ToString().ToLowerInvariant(), "Integration"),

            [GlobalSettingKeys.NotificationSmsCustomerOpsEnabled] = (snapshot.NotificationSmsCustomerOpsEnabled.ToString().ToLowerInvariant(), "Notification"),

            [GlobalSettingKeys.NotificationSmsWelcomeEnabled] = (snapshot.NotificationSmsWelcomeEnabled.ToString().ToLowerInvariant(), "Notification"),

            [GlobalSettingKeys.NotificationSmsWelcomeTemplateAr] = (snapshot.NotificationSmsWelcomeTemplateAr ?? "", "Notification"),

            [GlobalSettingKeys.NotificationSmsWelcomeTemplateEn] = (snapshot.NotificationSmsWelcomeTemplateEn ?? "", "Notification"),

        };

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

            IsDemoVersion = Bool(GlobalSettingKeys.IsDemoVersion, true, map),

            JwtAccessTokenMinutes = Int(GlobalSettingKeys.JwtAccessTokenMinutes, 60, map),

            IntegrationHuaweiEnabled = Bool(GlobalSettingKeys.IntegrationHuaweiEnabled, true, map),

            IntegrationHlrEnabled = Bool(GlobalSettingKeys.IntegrationHlrEnabled, true, map),

            IntegrationSmsEnabled = Bool(GlobalSettingKeys.IntegrationSmsEnabled, true, map),

            IntegrationInEnabled = Bool(GlobalSettingKeys.IntegrationInEnabled, true, map),

            IntegrationInGatewayUrl = Str(GlobalSettingKeys.IntegrationInGatewayUrl, "https://in-gateway.syriatel.local/api/v1", map),

            IntegrationInTimeoutMilliseconds = Int(GlobalSettingKeys.IntegrationInTimeoutMilliseconds, 5000, map),

            IntegrationInSimulateRealTimeDeduction = Bool(GlobalSettingKeys.IntegrationInSimulateRealTimeDeduction, true, map),

            NotificationSmsCustomerOpsEnabled = Bool(GlobalSettingKeys.NotificationSmsCustomerOpsEnabled, true, map),

            NotificationSmsWelcomeEnabled = Bool(GlobalSettingKeys.NotificationSmsWelcomeEnabled, true, map),

            NotificationSmsWelcomeTemplateAr = Str(

                GlobalSettingKeys.NotificationSmsWelcomeTemplateAr,

                "مرحباً بك في سيريتل. تم تفعيل خطك {Msisdn} بنجاح.",

                map),

            NotificationSmsWelcomeTemplateEn = Str(

                GlobalSettingKeys.NotificationSmsWelcomeTemplateEn,

                "Welcome to Syriatel. Your line {Msisdn} is now active.",

                map),

            Telecom = TelecomBusinessRulesMapper.FromDictionary(map),

        };

    }

}


