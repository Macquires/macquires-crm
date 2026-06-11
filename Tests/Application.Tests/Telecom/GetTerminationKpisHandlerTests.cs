using Application.Common.Telecom.Termination;
using Application.Features.TelecomManager.Queries;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class GetTerminationKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_termination_operations_for_today()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            Op("TRM-1", TelecomOperationStatus.Completed, TerminationWellKnown.Voluntary, "A", now, 10_000m),
            Op("TRM-2", TelecomOperationStatus.Failed, TerminationWellKnown.Fraud, "B", now, null),
            Op("TRM-3", TelecomOperationStatus.PendingDocuments, TerminationWellKnown.Fraud, "B", now, null, "BackOffice"));
        await ctx.SaveChangesAsync();

        var handler = new GetTerminationKpisHandler(ctx);
        var result = await handler.Handle(
            new GetTerminationKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(3, result.TotalToday);
        Assert.Equal(1, result.CompletedToday);
        Assert.Equal(1, result.FailedToday);
        Assert.Equal(1, result.PendingBackOffice);
        Assert.Equal(1, result.VoluntaryToday);
        Assert.Equal(10_000m, result.FinalBillTotalToday);
        Assert.True(result.TopReasons.Count >= 1);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static TelecomOperationRequest Op(
        string number,
        TelecomOperationStatus status,
        string terminationType,
        string reason,
        DateTime created,
        decimal? finalBill,
        string? approval = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.Termination,
            Status = status,
            TerminationType = terminationType,
            TerminationReason = reason,
            CreatedAtUtc = created,
            ConfirmedAtUtc = status == TelecomOperationStatus.Completed ? created.AddMinutes(5) : null,
            FinalBillAmount = finalBill,
            ApprovalLevelRequired = approval,
            SubscriberProfileId = "prof-seed",
            IsDeleted = false,
        };
}
