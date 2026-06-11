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

public class ChangeGsmNotificationHandlerTests
{
    [Fact]
    public async Task Handle_skips_sms_when_customer_ops_disabled()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new DataContext(options, TestOperatorContext.Instance);
        ctx.GlobalSetting.Add(new GlobalSetting
        {
            Key = GlobalSettingKeys.NotificationSmsCustomerOpsEnabled,
            Value = "false",
            Category = "Notification",
        });
        await ctx.SaveChangesAsync();

        var settings = new GlobalSettingsProvider(ctx, new MemoryCache(new MemoryCacheOptions()));
        var sms = new CaptureSms();
        var handler = new ChangeGsmNotificationHandler(sms, settings, NullLogger<ChangeGsmNotificationHandler>.Instance);

        await handler.Handle(
            new TelecomOperationStatusChangedNotification(
                Guid.CreateVersion7().ToString(),
                TelecomOperationKind.ChangeGsmType,
                TelecomOperationStatus.Draft,
                TelecomOperationStatus.Completed,
                null,
                "0999111222",
                null),
            CancellationToken.None);

        Assert.Equal(0, sms.SendCount);
    }

    private sealed class CaptureSms : ISmsGatewayIntegration
    {
        public int SendCount { get; private set; }

        public Task<BillingProvisionResult> SendAsync(string phoneNumber, string body, CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(new BillingProvisionResult(true, "ok"));
        }
    }
}
