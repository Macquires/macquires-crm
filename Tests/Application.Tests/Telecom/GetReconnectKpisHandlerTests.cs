using Application.Common.Telecom.Reconnect;
using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class GetReconnectKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_reconnect_operations_for_today()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            Op("RCN-1", TelecomOperationStatus.Completed, "Paid", ReconnectWellKnown.Payment, now, true),
            Op("RCN-2", TelecomOperationStatus.Failed, "Retry", ReconnectWellKnown.Customer, now, false),
            Op("RCN-3", TelecomOperationStatus.PendingDocuments, "Fraud", ReconnectWellKnown.Fraud, now, false, "BackOffice"));
        await ctx.SaveChangesAsync();

        var handler = new GetReconnectKpisHandler(ctx);
        var result = await handler.Handle(
            new GetReconnectKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(3, result.TotalToday);
        Assert.Equal(1, result.CompletedToday);
        Assert.Equal(1, result.FailedToday);
        Assert.Equal(1, result.PendingBackOffice);
        Assert.Equal(1, result.PaymentClearedToday);
        Assert.True(result.TopReasons.Count >= 1);
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
        string reason,
        string clearance,
        DateTime created,
        bool fraudClearance,
        string? approval = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.Reconnect,
            Status = status,
            ReconnectReason = reason,
            ClearanceType = clearance,
            FraudClearanceConfirmed = fraudClearance,
            CreatedAtUtc = created,
            ConfirmedAtUtc = status == TelecomOperationStatus.Completed ? created.AddMinutes(5) : null,
            ApprovalLevelRequired = approval,
            SubscriberProfileId = "prof-rcn",
            IsDeleted = false,
        };
}
