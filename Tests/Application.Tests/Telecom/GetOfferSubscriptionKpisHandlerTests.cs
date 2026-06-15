using Application.Features.TelecomManager.Queries;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class GetOfferSubscriptionKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_mgr_and_vas_operations()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            TelecomTestEntityFactory.Operation(op =>
            {
                op.Number = "MGR-1";
                op.Kind = TelecomOperationKind.Migration;
                op.Status = TelecomOperationStatus.Completed;
                op.TargetOfferName = "ميكس 500";
                op.CreatedAtUtc = now;
                op.ConfirmedAtUtc = now.AddMinutes(2);
                op.SubscriberProfileId = "p1";
            }),
            TelecomTestEntityFactory.Operation(op =>
            {
                op.Number = "MGR-2";
                op.Kind = TelecomOperationKind.Migration;
                op.Status = TelecomOperationStatus.Failed;
                op.CreatedAtUtc = now;
                op.SubscriberProfileId = "p1";
            }),
            TelecomTestEntityFactory.Operation(op =>
            {
                op.Number = "VAS-1";
                op.Kind = TelecomOperationKind.ServiceModification;
                op.Status = TelecomOperationStatus.Completed;
                op.Notes = "Activate VAS VAS_ROAMING";
                op.CreatedAtUtc = now;
                op.SubscriberProfileId = "p1";
            }));
        await ctx.SaveChangesAsync();

        var handler = new GetOfferSubscriptionKpisHandler(ctx, StubOperationalAnalyticsScopeService.Instance);
        var result = await handler.Handle(
            new GetOfferSubscriptionKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(2, result.MigrationTotalToday);
        Assert.Equal(1, result.MigrationCompletedToday);
        Assert.Equal(1, result.MigrationFailedToday);
        Assert.Equal(1, result.VasActivateToday);
        Assert.Equal(0, result.VasDeactivateToday);
        Assert.Single(result.TopMigratedOffers);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }
}
