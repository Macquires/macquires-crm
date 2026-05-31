using Application.Common.Settings;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.SeedManager.Systems;

public class GlobalSettingSeeder
{
    private readonly DataContext _context;
    private readonly IConfiguration _configuration;

    public GlobalSettingSeeder(DataContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task GenerateDataAsync()
    {
        var defaults = BuildDefaults();
        foreach (var (key, value, category) in defaults)
        {
            if (await _context.GlobalSetting.AnyAsync(x => x.Key == key))
            {
                continue;
            }

            await _context.GlobalSetting.AddAsync(new GlobalSetting
            {
                Key = key,
                Value = value,
                Category = category,
                UpdatedAtUtc = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();
    }

    private IReadOnlyList<(string Key, string Value, string Category)> BuildDefaults()
    {
        var jwtMinutes = _configuration.GetSection("Jwt").GetValue<int?>("ExpireInMinute") ?? 60;
        return
        [
            (GlobalSettingKeys.MaintenanceMode, "false", "Maintenance"),
            (GlobalSettingKeys.MaintenanceMessageAr, "نعتذر، النظام قيد التحديث حالياً. يرجى المحاولة لاحقاً.", "Maintenance"),
            (GlobalSettingKeys.MaintenanceMessageEn, "The system is undergoing maintenance. Please try again later.", "Maintenance"),
            (GlobalSettingKeys.MaintenanceBypassIPs, "127.0.0.1", "Maintenance"),
            (GlobalSettingKeys.IsStrictPersonaMode, "true", "Security"),
            (GlobalSettingKeys.JwtAccessTokenMinutes, jwtMinutes.ToString(), "Security"),
            (GlobalSettingKeys.IntegrationHuaweiEnabled, "true", "Integration"),
            (GlobalSettingKeys.IntegrationHlrEnabled, "true", "Integration"),
            (GlobalSettingKeys.IntegrationSmsEnabled, "true", "Integration"),
        ];
    }
}
