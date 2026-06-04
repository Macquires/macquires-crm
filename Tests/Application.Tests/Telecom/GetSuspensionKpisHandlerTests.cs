using Application.Common.Telecom.Suspension;
using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class GetSuspensionKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_suspension_operations_for_today()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            Op("SUS-1", TelecomOperationStatus.Completed, SuspensionWellKnown.CustomerRequest, "A", now, false),
            Op("SUS-2", TelecomOperationStatus.Failed, SuspensionWellKnown.Billing, "B", now, true),
            Op("SUS-3", TelecomOperationStatus.PendingDocuments, SuspensionWellKnown.Fraud, "C", now, false, "BackOffice"));
        await ctx.SaveChangesAsync();

        var handler = new GetSuspensionKpisHandler(ctx);
        var result = await handler.Handle(
            new GetSuspensionKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(3, result.TotalToday);
        Assert.Equal(1, result.CompletedToday);
        Assert.Equal(1, result.FailedToday);
        Assert.Equal(1, result.PendingBackOffice);
        Assert.Equal(1, result.FraudToday);
        Assert.Equal(1, result.AutoReconnectEnabledToday);
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
        string suspensionType,
        string reason,
        DateTime created,
        bool autoReconnect,
        string? approval = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.TemporarySuspension,
            Status = status,
            SuspensionType = suspensionType,
            SuspensionReason = reason,
            AutoReconnectEnabled = autoReconnect,
            CreatedAtUtc = created,
            ConfirmedAtUtc = status == TelecomOperationStatus.Completed ? created.AddMinutes(5) : null,
            ApprovalLevelRequired = approval,
            SubscriberProfileId = "prof-sus",
            IsDeleted = false,
        };
}
