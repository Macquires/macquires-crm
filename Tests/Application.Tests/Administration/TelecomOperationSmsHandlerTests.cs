using Application.Common.Events;
using Application.Common.Integrations;
using Application.Common.Settings;
using Application.Features.TelecomManager.Events;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Administration;

using Application.Tests.TestSupport;

public class TelecomOperationSmsHandlerTests
{
    [Fact]
    public async Task Handle_skips_sms_when_customer_ops_disabled()
    {
        var (handler, sms, ctx) = await CreateHandlerAsync(customerOps: false, welcome: true);
        await handler.Handle(ActivationNotification("0999111222"), CancellationToken.None);

        Assert.Equal(0, sms.SendCount);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task Handle_skips_sms_when_welcome_disabled()
    {
        var (handler, sms, ctx) = await CreateHandlerAsync(customerOps: true, welcome: false);
        await handler.Handle(ActivationNotification("0999111222"), CancellationToken.None);

        Assert.Equal(0, sms.SendCount);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task Handle_sends_custom_welcome_template_when_enabled()
    {
        var (handler, sms, ctx) = await CreateHandlerAsync(
            customerOps: true,
            welcome: true,
            templateAr: "أهلاً {Msisdn} في سيريتل");

        await handler.Handle(ActivationNotification("0999777666"), CancellationToken.None);

        Assert.Equal(1, sms.SendCount);
        Assert.Equal("أهلاً 0999777666 في سيريتل", sms.LastBody);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task Handle_skips_non_activation_operations()
    {
        var (handler, sms, ctx) = await CreateHandlerAsync(customerOps: true, welcome: true);
        await handler.Handle(
            new TelecomOperationProvisionedNotification(
                TelecomOperationKind.SimSwap,
                Guid.CreateVersion7().ToString(),
                "0999111222",
                null,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(0, sms.SendCount);
        await ctx.DisposeAsync();
    }

    private static TelecomOperationProvisionedNotification ActivationNotification(string msisdn) =>
        new(
            TelecomOperationKind.NewActivation,
            Guid.CreateVersion7().ToString(),
            msisdn,
            null,
            null,
            null);

    private static async Task<(TelecomOperationSmsHandler Handler, CaptureSmsGateway Sms, DataContext Ctx)> CreateHandlerAsync(
        bool customerOps,
        bool welcome,
        string? templateAr = null)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new DataContext(options, TestOperatorContext.Instance);

        await ctx.GlobalSetting.AddRangeAsync(
            new GlobalSetting
            {
                Key = GlobalSettingKeys.NotificationSmsCustomerOpsEnabled,
                Value = customerOps.ToString().ToLowerInvariant(),
                Category = "Notification",
            },
            new GlobalSetting
            {
                Key = GlobalSettingKeys.NotificationSmsWelcomeEnabled,
                Value = welcome.ToString().ToLowerInvariant(),
                Category = "Notification",
            });

        if (templateAr != null)
        {
            await ctx.GlobalSetting.AddAsync(new GlobalSetting
            {
                Key = GlobalSettingKeys.NotificationSmsWelcomeTemplateAr,
                Value = templateAr,
                Category = "Notification",
            });
        }

        await ctx.SaveChangesAsync();

        var settings = new GlobalSettingsProvider(ctx, new MemoryCache(new MemoryCacheOptions()));
        var sms = new CaptureSmsGateway();
        var handler = new TelecomOperationSmsHandler(sms, settings, NullLogger<TelecomOperationSmsHandler>.Instance);
        return (handler, sms, ctx);
    }

    private sealed class CaptureSmsGateway : ISmsGatewayIntegration
    {
        public int SendCount { get; private set; }
        public string? LastBody { get; private set; }

        public Task<BillingProvisionResult> SendAsync(string phoneNumber, string body, CancellationToken cancellationToken = default)
        {
            SendCount++;
            LastBody = body;
            return Task.FromResult(new BillingProvisionResult(true, "sent"));
        }
    }
}
