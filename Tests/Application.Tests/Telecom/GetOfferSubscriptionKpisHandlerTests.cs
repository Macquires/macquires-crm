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
            new TelecomOperationRequest
            {
                Id = Guid.NewGuid().ToString(),
                Number = "MGR-1",
                Kind = TelecomOperationKind.Migration,
                Status = TelecomOperationStatus.Completed,
                TargetOfferName = "ميكس 500",
                CreatedAtUtc = now,
                ConfirmedAtUtc = now.AddMinutes(2),
                SubscriberProfileId = "p1",
                IsDeleted = false,
            },
            new TelecomOperationRequest
            {
                Id = Guid.NewGuid().ToString(),
                Number = "MGR-2",
                Kind = TelecomOperationKind.Migration,
                Status = TelecomOperationStatus.Failed,
                CreatedAtUtc = now,
                SubscriberProfileId = "p1",
                IsDeleted = false,
            },
            new TelecomOperationRequest
            {
                Id = Guid.NewGuid().ToString(),
                Number = "VAS-1",
                Kind = TelecomOperationKind.ServiceModification,
                Status = TelecomOperationStatus.Completed,
                Notes = "Activate VAS VAS_ROAMING",
                CreatedAtUtc = now,
                SubscriberProfileId = "p1",
                IsDeleted = false,
            });
        await ctx.SaveChangesAsync();

        var handler = new GetOfferSubscriptionKpisHandler(ctx);
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
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }
}
