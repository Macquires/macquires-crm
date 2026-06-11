using Application.Common.Integrations;
using Application.Common.Settings;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.Settings;
using Infrastructure.TelecomIntegrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class IntelligentNetworkMockServiceTests
{
    [Fact]
    public async Task Provision_then_credit_updates_prepaid_balance()
    {
        var (service, _) = await CreateServiceAsync(inEnabled: true, timeoutMs: 8000);

        var msisdn = "0999111220";
        var provision = await service.ProvisionPrepaidSubscriberAsync(msisdn, "417012345678901", "PREPAID_VOICE");
        Assert.True(provision.Success);

        var credit = await service.CreditPrepaidBalanceAsync(msisdn, 25_000m, "TX-001");
        Assert.True(credit.Success);
        Assert.Equal(25_000m, credit.BalanceAfter);

        var balance = await service.GetPrepaidBalanceAsync(msisdn);
        Assert.True(balance.Success);
        Assert.Equal(25_000m, balance.BalanceAfter);
    }

    [Fact]
    public async Task When_in_disabled_returns_queued_fallback()
    {
        var (service, _) = await CreateServiceAsync(inEnabled: false, timeoutMs: 5000);

        var result = await service.ProvisionPrepaidSubscriberAsync("0999333444", "4170999888777", "PREPAID_VOICE");

        Assert.True(result.Success);
        Assert.True(result.QueuedForSync);
    }

    private static async Task<(HuaweiIntelligentNetworkMockService Service, DataContext Context)> CreateServiceAsync(
        bool inEnabled,
        int timeoutMs)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new DataContext(options, TestOperatorContext.Instance);
        context.GlobalSetting.AddRange(
            new GlobalSetting { Key = GlobalSettingKeys.IntegrationInEnabled, Value = inEnabled.ToString().ToLowerInvariant(), Category = "Integration" },
            new GlobalSetting { Key = GlobalSettingKeys.IntegrationInTimeoutMilliseconds, Value = timeoutMs.ToString(), Category = "Integration" },
            new GlobalSetting { Key = GlobalSettingKeys.IntegrationInGatewayUrl, Value = "https://in.test/api", Category = "Integration" },
            new GlobalSetting { Key = GlobalSettingKeys.IntegrationInSimulateRealTimeDeduction, Value = "true", Category = "Integration" });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var settings = new GlobalSettingsProvider(context, cache);
        var enablement = new IntegrationEnablement(settings);
        var service = new HuaweiIntelligentNetworkMockService(
            settings,
            new NoOpIntegrationLogWriter(),
            enablement,
            NullLogger<HuaweiIntelligentNetworkMockService>.Instance);

        return (service, context);
    }

    private sealed class NoOpIntegrationLogWriter : ITelecomIntegrationLogWriter
    {
        public Task WriteAsync(
            TelecomIntegrationSystem system,
            string operationName,
            string? msisdn,
            string? requestPayload,
            string? responsePayload,
            bool isSuccess,
            string? responseStatusCode,
            long executionTimeMs,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
