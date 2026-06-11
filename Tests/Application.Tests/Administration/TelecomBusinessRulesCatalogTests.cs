using Application.Common.Settings;
using Application.Common.Settings.Telecom;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Tests.Administration;

using Application.Tests.TestSupport;

public class TelecomBusinessRulesCatalogTests
{
    [Fact]
    public void Catalog_contains_all_telecom_keys()
    {
        Assert.True(TelecomBusinessRulesCatalog.All.Count >= 31);
        Assert.Contains(TelecomBusinessRulesCatalog.All, x => x.Key == GlobalSettingKeys.TelecomRefundDualApprovalThresholdSyp);
        Assert.Contains(TelecomBusinessRulesCatalog.All, x => x.Key == GlobalSettingKeys.TelecomInventoryMsisdnQuarantineDays);
        Assert.Contains(TelecomBusinessRulesCatalog.All, x => x.Key == GlobalSettingKeys.TelecomActivationChannelShowroomLabelEn);
    }

    [Fact]
    public async Task GetCatalogDecimalAsync_reads_refund_threshold_from_database()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DataContext(options, TestOperatorContext.Instance);
        context.GlobalSetting.Add(new GlobalSetting
        {
            Key = GlobalSettingKeys.TelecomRefundDualApprovalThresholdSyp,
            Value = "750000",
            Category = TelecomBusinessRulesCatalog.CategoryRefund,
        });
        await context.SaveChangesAsync();

        var provider = new GlobalSettingsProvider(context, new MemoryCache(new MemoryCacheOptions()));
        var def = TelecomBusinessRulesCatalog.FindByKey(GlobalSettingKeys.TelecomRefundDualApprovalThresholdSyp)!;

        var amount = await provider.GetCatalogDecimalAsync(def);

        Assert.Equal(750_000m, amount);
    }

    [Fact]
    public async Task SaveSnapshot_seeds_telecom_keys_when_telecom_provided()
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
            Telecom = new TelecomBusinessRulesDto
            {
                Inventory = { MsisdnQuarantineDays = 45, SimQuarantineDays = 60, MsisdnReservationMinutes = 20, MsisdnReservationHours = 12 },
                Refund = { DualApprovalThresholdSyp = 300_000m },
            },
        };

        await service.SaveSnapshotAsync(snapshot, "admin");
        var loaded = await service.GetSnapshotAsync();

        Assert.Equal(45, loaded.Telecom!.Inventory.MsisdnQuarantineDays);
        Assert.Equal(60, loaded.Telecom.Inventory.SimQuarantineDays);
        Assert.Equal(20, loaded.Telecom.Inventory.MsisdnReservationMinutes);
        Assert.Equal(12, loaded.Telecom.Inventory.MsisdnReservationHours);
        Assert.Equal(300_000m, loaded.Telecom.Refund.DualApprovalThresholdSyp);

        var rows = await context.GlobalSetting.AsNoTracking().ToListAsync();
        Assert.Contains(rows, x => x.Key == GlobalSettingKeys.TelecomInventoryMsisdnReservationMinutes && x.Value == "20");
        Assert.Contains(rows, x => x.Key == GlobalSettingKeys.TelecomInventoryMsisdnQuarantineDays && x.Value == "45");
        Assert.Contains(rows, x => x.Key == GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual && x.Value == "5");
    }

    [Fact]
    public async Task SaveSnapshot_preserves_telecom_when_incoming_telecom_is_null()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DataContext(options, TestOperatorContext.Instance);
        var service = new GlobalSettingsAdminService(context, new MemoryCache(new MemoryCacheOptions()));

        await service.SaveSnapshotAsync(new GlobalSettingsSnapshotDto
        {
            MaintenanceMode = false,
            IsStrictPersonaMode = true,
            JwtAccessTokenMinutes = 60,
            Telecom = new TelecomBusinessRulesDto
            {
                Inventory = { MsisdnQuarantineDays = 77 },
            },
        }, "admin");

        await service.SaveSnapshotAsync(new GlobalSettingsSnapshotDto
        {
            MaintenanceMode = true,
            IsStrictPersonaMode = true,
            JwtAccessTokenMinutes = 60,
            Telecom = null,
        }, "admin");

        var loaded = await service.GetSnapshotAsync();
        Assert.True(loaded.MaintenanceMode);
        Assert.Equal(77, loaded.Telecom!.Inventory.MsisdnQuarantineDays);
    }

    [Fact]
    public void MergeSnapshots_keeps_existing_telecom_when_incoming_null()
    {
        var existing = new GlobalSettingsSnapshotDto
        {
            Telecom = new TelecomBusinessRulesDto { Refund = { DualApprovalThresholdSyp = 999_000m } },
        };
        var incoming = new GlobalSettingsSnapshotDto { MaintenanceMode = true, Telecom = null };

        var merged = GlobalSettingsAdminService.MergeSnapshots(existing, incoming);

        Assert.True(merged.MaintenanceMode);
        Assert.Equal(999_000m, merged.Telecom!.Refund.DualApprovalThresholdSyp);
    }
}
