using Application.Features.TelecomManager.Queries;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class GetSellingLineActivationKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_new_activation_operations()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            Op("ACT-1", TelecomOperationStatus.Completed, now, now.AddMinutes(3), null),
            Op("ACT-2", TelecomOperationStatus.Failed, now, null, "VAL-02-01"),
            Op("ACT-3", TelecomOperationStatus.PendingExternal, now, null, null));
        await ctx.SaveChangesAsync();

        var handler = new GetSellingLineActivationKpisHandler(ctx);
        var result = await handler.Handle(
            new GetSellingLineActivationKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(3, result.TotalVolume);
        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(1, result.PendingExternalCount);
        Assert.True(result.CompletionRatePercent > 0);
        Assert.True(result.FalloutRatePercent > 0);
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
        DateTime created,
        DateTime? confirmed,
        string? overrideReason) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.NewActivation,
            Status = status,
            CreatedAtUtc = created,
            ConfirmedAtUtc = confirmed,
            OverrideReasonCode = overrideReason,
            SubscriberProfileId = "prof-seed",
            IsDeleted = false,
        };
}
