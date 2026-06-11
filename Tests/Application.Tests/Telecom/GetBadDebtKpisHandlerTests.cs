using Application.Common.Telecom.BadDebt;
using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class GetBadDebtKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_bad_debt_operations_for_today()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            Op("BDR-1", TelecomOperationStatus.Completed, BadDebtWellKnown.PaymentRecorded, 8000m, null, now),
            Op("BDR-2", TelecomOperationStatus.Completed, BadDebtWellKnown.WriteOffPartial, null, 3000m, now),
            Op("BDR-3", TelecomOperationStatus.Failed, BadDebtWellKnown.DunningEscalation, null, null, now),
            Op("BDR-4", TelecomOperationStatus.PendingDocuments, BadDebtWellKnown.WriteOffFull, null, 1000m, now, "BackOffice"));
        await ctx.SaveChangesAsync();

        var handler = new GetBadDebtKpisHandler(ctx);
        var result = await handler.Handle(
            new GetBadDebtKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(4, result.TotalToday);
        Assert.Equal(2, result.CompletedToday);
        Assert.Equal(1, result.FailedToday);
        Assert.Equal(1, result.PendingBackOffice);
        Assert.Equal(8000m, result.CollectedAmountToday);
        Assert.Equal(3000m, result.WriteOffAmountToday);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static TelecomOperationRequest Op(
        string number,
        TelecomOperationStatus status,
        string action,
        decimal? collected,
        decimal? writeOff,
        DateTime created,
        string? approval = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.BadDebtRecovery,
            Status = status,
            CollectionAction = action,
            CollectedAmount = collected,
            WriteOffAmount = writeOff,
            CreatedAtUtc = created,
            ApprovalLevelRequired = approval,
            SubscriberProfileId = "prof-bdr",
            IsDeleted = false,
        };
}
