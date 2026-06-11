using Application.Common.Settings;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Tests.Administration;

using Application.Tests.TestSupport;

public class GlobalSettingsNotificationTests
{
    [Fact]
    public async Task SaveSnapshot_persists_notification_fields()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DataContext(options, TestOperatorContext.Instance);
        var service = new GlobalSettingsAdminService(context, new MemoryCache(new MemoryCacheOptions()));

        var snapshot = new GlobalSettingsSnapshotDto
        {
            MaintenanceMode = false,
            IsStrictPersonaMode = true,
            JwtAccessTokenMinutes = 60,
            NotificationSmsCustomerOpsEnabled = false,
            NotificationSmsWelcomeEnabled = true,
            NotificationSmsWelcomeTemplateAr = "أهلاً {Msisdn}",
            NotificationSmsWelcomeTemplateEn = "Hi {Msisdn}",
        };

        await service.SaveSnapshotAsync(snapshot, "tester");

        var rows = await context.GlobalSetting.AsNoTracking().ToListAsync();
        Assert.Contains(rows, x => x.Key == GlobalSettingKeys.NotificationSmsCustomerOpsEnabled && x.Value == "false");
        Assert.Contains(rows, x => x.Key == GlobalSettingKeys.NotificationSmsWelcomeEnabled && x.Value == "true");
        Assert.Contains(rows, x => x.Key == GlobalSettingKeys.NotificationSmsWelcomeTemplateAr && x.Value == "أهلاً {Msisdn}");

        var loaded = await service.GetSnapshotAsync();
        Assert.False(loaded.NotificationSmsCustomerOpsEnabled);
        Assert.True(loaded.NotificationSmsWelcomeEnabled);
        Assert.Equal("أهلاً {Msisdn}", loaded.NotificationSmsWelcomeTemplateAr);
    }

    [Fact]
    public async Task ResolveWelcomeBody_replaces_msisdn_placeholder()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DataContext(options, TestOperatorContext.Instance);
        context.GlobalSetting.Add(new GlobalSetting
        {
            Key = GlobalSettingKeys.NotificationSmsWelcomeTemplateAr,
            Value = "خطك {Msisdn} جاهز",
            Category = "Notification",
        });
        await context.SaveChangesAsync();

        var provider = new GlobalSettingsProvider(context, new MemoryCache(new MemoryCacheOptions()));
        var body = await NotificationSmsGate.ResolveWelcomeBodyAsync(provider, "0999888777");

        Assert.Equal("خطك 0999888777 جاهز", body);
    }

    [Fact]
    public async Task SaveSnapshot_roundtrip_preserves_security_and_maintenance_fields()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DataContext(options, TestOperatorContext.Instance);
        var service = new GlobalSettingsAdminService(context, new MemoryCache(new MemoryCacheOptions()));

        var snapshot = new GlobalSettingsSnapshotDto
        {
            MaintenanceMode = true,
            MaintenanceMessageAr = "صيانة",
            MaintenanceMessageEn = "Maintenance",
            MaintenanceBypassIPs = "10.0.0.1",
            IsStrictPersonaMode = false,
            JwtAccessTokenMinutes = 120,
            IntegrationHuaweiEnabled = false,
            IntegrationInEnabled = true,
            IntegrationInGatewayUrl = "https://in.test/api",
            IntegrationInTimeoutMilliseconds = 3000,
            IntegrationInSimulateRealTimeDeduction = false,
            IntegrationHlrEnabled = true,
            IntegrationSmsEnabled = false,
            NotificationSmsCustomerOpsEnabled = true,
            NotificationSmsWelcomeEnabled = false,
        };

        await service.SaveSnapshotAsync(snapshot, "admin");
        var loaded = await service.GetSnapshotAsync();

        Assert.True(loaded.MaintenanceMode);
        Assert.Equal("صيانة", loaded.MaintenanceMessageAr);
        Assert.False(loaded.IsStrictPersonaMode);
        Assert.Equal(120, loaded.JwtAccessTokenMinutes);
        Assert.False(loaded.IntegrationHuaweiEnabled);
        Assert.True(loaded.IntegrationInEnabled);
        Assert.Equal("https://in.test/api", loaded.IntegrationInGatewayUrl);
        Assert.Equal(3000, loaded.IntegrationInTimeoutMilliseconds);
        Assert.False(loaded.IntegrationInSimulateRealTimeDeduction);
        Assert.False(loaded.IntegrationSmsEnabled);
        Assert.False(loaded.NotificationSmsWelcomeEnabled);
    }

    [Fact]
    public async Task IsCustomerOpsEnabled_defaults_true_when_missing()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DataContext(options, TestOperatorContext.Instance);
        var provider = new GlobalSettingsProvider(context, new MemoryCache(new MemoryCacheOptions()));

        Assert.True(await NotificationSmsGate.IsCustomerOpsEnabledAsync(provider));
    }
}
