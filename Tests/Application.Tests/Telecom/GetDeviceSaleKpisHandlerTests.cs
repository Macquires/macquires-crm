using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class GetDeviceSaleKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_device_sale_operations_for_today()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            Op("DEV-1", TelecomOperationStatus.Completed, DeviceSaleType.Installment, now, null),
            Op("DEV-2", TelecomOperationStatus.Completed, DeviceSaleType.Cash, now, "OVERRIDE"),
            Op("DEV-3", TelecomOperationStatus.Failed, DeviceSaleType.Installment, now, null, "Stock unavailable"));
        await ctx.SaveChangesAsync();

        var handler = new GetDeviceSaleKpisHandler(ctx);
        var result = await handler.Handle(
            new GetDeviceSaleKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(3, result.TotalVolume);
        Assert.Equal(2, result.CompletedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(2, result.InstallmentCount);
        Assert.Equal(1, result.CashCount);
        Assert.Equal(1, result.ManualOverrideCount);
        Assert.Equal(1, result.RejectionReasons.Count);
        Assert.True(result.CompletionRatePercent > 0);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options);
    }

    private static TelecomOperationRequest Op(
        string number,
        TelecomOperationStatus status,
        DeviceSaleType saleType,
        DateTime created,
        string? overrideCode,
        string? failNote = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.DeviceSale,
            Status = status,
            DeviceSaleType = saleType,
            DeviceOverrideReasonCode = overrideCode,
            DeviceFinancingNoteAr = failNote,
            CreatedAtUtc = created,
            ConfirmedAtUtc = status == TelecomOperationStatus.Completed ? created.AddMinutes(20) : null,
            SubscriberProfileId = "prof-dev",
            IsDeleted = false,
        };
}
