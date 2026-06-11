using Application.Features.ProductManager.Queries;
using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class GetEligibleVasOfferingsHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Registry_Vas_For_Subscriber()
    {
        await using var ctx = CreateContext();
        var profileId = Guid.CreateVersion7().ToString();
        var msisdnId = Guid.CreateVersion7().ToString();

        ctx.TelecomValueAddedService.Add(new TelecomValueAddedService
        {
            Id = Guid.CreateVersion7().ToString(),
            ServiceCode = "VAS_TEST",
            NameAr = "اختبار",
            HlrCommandTemplate = "TPL",
            IsActive = true,
            SortOrder = 1,
            IsDeleted = false,
        });

        ctx.TelecomSubscription.Add(new TelecomSubscription
        {
            Id = Guid.CreateVersion7().ToString(),
            SubscriberProfileId = profileId,
            MsisdnAssetId = msisdnId,
            SubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
            IsPrimaryLine = true,
            IsDeleted = false,
        });

        await ctx.SaveChangesAsync();

        var handler = new GetEligibleVasOfferingsHandler(ctx);
        var result = await handler.Handle(
            new GetEligibleVasOfferingsRequest
            {
                SubscriberProfileId = profileId,
                MsisdnAssetId = msisdnId,
            },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal("VAS_TEST", result.Data[0].ServiceCode);
        Assert.Equal("Registry", result.Data[0].Source);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }
}
