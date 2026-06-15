using Application.Common.Telecom.OperationCreate;
using Application.Common.Telecom.OfferSubscription;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public sealed class MigrationEffectiveDateTests
{
    [Fact]
    public async Task ApplyToEntity_uses_request_date_when_provided()
    {
        var scheduled = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var entity = new TelecomOperationRequest { Kind = TelecomOperationKind.Migration };
        var request = new CreateTelecomOperationRequest
        {
            Kind = TelecomOperationKind.Migration,
            MigrationEffectiveDateUtc = scheduled,
        };
        var validation = new OperationCreateContext(request, "actor-1")
        {
            MigrationEligibility = new OfferSubscriptionEligibilityResult(
                Allowed: true,
                MessageAr: "ok",
                Msisdn: null,
                MsisdnAssetId: null,
                PriorProductId: "prod-old",
                PriorProductOfferingId: "offer-old",
                ValidationCode: "OK"),
        };

        var build = new OperationCreateBuildContext(validation, entity, null, null, "actor-1");
        var strategy = new MigrationCreateStrategy(null!, null!, null!);
        await strategy.ApplyToEntityAsync(build, CancellationToken.None);

        Assert.Equal(scheduled, entity.MigrationEffectiveDateUtc);
        Assert.Equal("prod-old", entity.PriorProductId);
    }

    [Fact]
    public async Task ApplyToEntity_defaults_to_utc_now_when_date_omitted()
    {
        var before = DateTime.UtcNow.AddSeconds(-2);
        var entity = new TelecomOperationRequest { Kind = TelecomOperationKind.Migration };
        var request = new CreateTelecomOperationRequest { Kind = TelecomOperationKind.Migration };
        var validation = new OperationCreateContext(request, "actor-1")
        {
            MigrationEligibility = new OfferSubscriptionEligibilityResult(
                Allowed: true,
                MessageAr: "ok",
                Msisdn: null,
                MsisdnAssetId: null,
                PriorProductId: "p",
                PriorProductOfferingId: "o",
                ValidationCode: "OK"),
        };

        var build = new OperationCreateBuildContext(validation, entity, null, null, "actor-1");
        var strategy = new MigrationCreateStrategy(null!, null!, null!);
        await strategy.ApplyToEntityAsync(build, CancellationToken.None);
        var after = DateTime.UtcNow.AddSeconds(2);

        Assert.NotNull(entity.MigrationEffectiveDateUtc);
        Assert.InRange(entity.MigrationEffectiveDateUtc!.Value, before, after);
    }
}
