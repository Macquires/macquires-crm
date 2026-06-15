using Application.Common.Settings;

using Application.Common.Settings.Telecom;

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

        var core = new List<(string Key, string Value, string Category)>

        {

            (GlobalSettingKeys.MaintenanceMode, "false", "Maintenance"),

            (GlobalSettingKeys.MaintenanceMessageAr, "نعتذر، النظام قيد التحديث حالياً. يرجى المحاولة لاحقاً.", "Maintenance"),

            (GlobalSettingKeys.MaintenanceMessageEn, "The system is undergoing maintenance. Please try again later.", "Maintenance"),

            (GlobalSettingKeys.MaintenanceBypassIPs, "127.0.0.1", "Maintenance"),

            (GlobalSettingKeys.IsStrictPersonaMode, "true", "Security"),

            (GlobalSettingKeys.IsDemoVersion, "true", "System"),

            (GlobalSettingKeys.JwtAccessTokenMinutes, jwtMinutes.ToString(), "Security"),

            (GlobalSettingKeys.IntegrationHuaweiEnabled, "true", "Integration"),

            (GlobalSettingKeys.IntegrationHlrEnabled, "true", "Integration"),

            (GlobalSettingKeys.IntegrationSmsEnabled, "true", "Integration"),

            (GlobalSettingKeys.IntegrationInEnabled, "true", "Integration"),

            (GlobalSettingKeys.IntegrationInGatewayUrl, "https://in-gateway.syriatel.local/api/v1", "Integration"),

            (GlobalSettingKeys.IntegrationInTimeoutMilliseconds, "5000", "Integration"),

            (GlobalSettingKeys.IntegrationInSimulateRealTimeDeduction, "true", "Integration"),

            (GlobalSettingKeys.NotificationSmsCustomerOpsEnabled, "true", "Notification"),

            (GlobalSettingKeys.NotificationSmsWelcomeEnabled, "true", "Notification"),

            (GlobalSettingKeys.NotificationSmsWelcomeTemplateAr, "مرحباً بك في سيريتل. تم تفعيل خطك {Msisdn} بنجاح.", "Notification"),

            (GlobalSettingKeys.NotificationSmsWelcomeTemplateEn, "Welcome to Syriatel. Your line {Msisdn} is now active.", "Notification"),

            (GlobalSettingKeys.ExecutiveDigestEmailEnabled, "false", "Notification"),

        };



        foreach (var def in TelecomBusinessRulesCatalog.All)

        {

            core.Add((def.Key, def.DefaultValue, def.Category));

        }



        // Legacy alias seeded alongside catalog.

        var maxLines = TelecomBusinessRulesCatalog.FindByKey(GlobalSettingKeys.TelecomActivationMaxLinesIndividual)?.DefaultValue ?? "5";

        if (core.All(x => x.Key != GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual))

        {

            core.Add((GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual, maxLines, TelecomBusinessRulesCatalog.CategoryActivation));

        }



        return core;

    }

}


