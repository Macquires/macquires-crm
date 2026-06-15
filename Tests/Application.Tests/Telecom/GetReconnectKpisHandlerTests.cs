using Application.Common.Telecom.Reconnect;
using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

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

        var handler = new GetReconnectKpisHandler(ctx, StubOperationalAnalyticsScopeService.Instance);
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
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static TelecomOperationRequest Op(
        string number,
        TelecomOperationStatus status,
        string reason,
        string clearance,
        DateTime created,
        bool fraudClearance,
        string? approval = null) =>
        TelecomTestEntityFactory.Operation(op =>
        {
            op.Number = number;
            op.Kind = TelecomOperationKind.Reconnect;
            op.Status = status;
            op.ReconnectReason = reason;
            op.ClearanceType = clearance;
            op.FraudClearanceConfirmed = fraudClearance;
            op.CreatedAtUtc = created;
            op.ConfirmedAtUtc = status == TelecomOperationStatus.Completed ? created.AddMinutes(5) : null;
            op.ApprovalLevelRequired = approval;
            op.SubscriberProfileId = "prof-rcn";
        });
}
