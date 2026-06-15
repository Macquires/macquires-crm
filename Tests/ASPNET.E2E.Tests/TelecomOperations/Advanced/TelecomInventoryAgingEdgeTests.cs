using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Application.Common.Telecom.Termination;
using Domain.Enums;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomInventoryAgingEdgeTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomInventoryAgingEdgeTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task NewActivation_RecentlyTerminatedMsisdn_BlockedDuringCoolingThenAllowedAfterRelease()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(12));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var activationSeed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.NewActivation,
            "aging-activation-primary",
            ct);

        var msisdnAssetId = activationSeed.Line.MsisdnAssetId;
        var msisdn = activationSeed.Line.Msisdn;
        string? firstProfileId = null;

        var activatePayload = E2ETelecomOperationSeeds.BuildCreatePayload(activationSeed);
        var activateOpId = await client.CreateOperationAsync(activatePayload, ct);
        await client.UploadDocumentAsync(activateOpId, ct);
        var activateCorrelation = await E2ETelecomOperationSeeds.GetCorrelationIdAsync(
            _fixture.AppFactory.Services,
            activateOpId,
            ct);
        _ = await client.ConfirmAsync(activateOpId, ct);
        await E2ETestDataHelper.WaitForOutboxProcessedAsync(
            _fixture.AppFactory.Services,
            activateCorrelation,
            TimeSpan.FromSeconds(90),
            ct);
        await WaitForTerminalStatusAsync(_fixture.AppFactory.Services, activateOpId, ct);

        firstProfileId = await E2EAdvancedTestHelper.GetSubscriberProfileIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            msisdnAssetId,
            ct);
        Assert.False(string.IsNullOrWhiteSpace(firstProfileId));

        var terminationPayload = new
        {
            kind = (int)TelecomOperationKind.Termination,
            subscriberProfileId = firstProfileId,
            msisdnAssetId,
            terminationType = TerminationWellKnown.Voluntary,
            terminationReason = "E2E aging test",
            retentionOfferOutcome = "Declined",
        };
        var terminationOpId = await client.CreateOperationAsync(terminationPayload, ct);
        await client.UploadDocumentAsync(terminationOpId, ct);
        var terminationCorrelation = await E2ETelecomOperationSeeds.GetCorrelationIdAsync(
            _fixture.AppFactory.Services,
            terminationOpId,
            ct);
        _ = await client.ConfirmAsync(terminationOpId, ct);
        await E2ETestDataHelper.WaitForOutboxProcessedAsync(
            _fixture.AppFactory.Services,
            terminationCorrelation,
            TimeSpan.FromSeconds(90),
            ct);
        await WaitForTerminalStatusAsync(_fixture.AppFactory.Services, terminationOpId, ct);

        var quarantinedStatus = await E2EAdvancedTestHelper.GetMsisdnPoolStatusAsync(
            _fixture.AppFactory.Services,
            msisdnAssetId,
            ct);
        Assert.Equal(MsisdnPoolStatus.Quarantined, quarantinedStatus);

        var recycleSeed = activationSeed with
        {
            ScenarioKey = "aging-recycle-attempt",
            Line = activationSeed.Line with { SubscriberProfileId = activationSeed.Line.SubscriberProfileId },
        };
        var blockedResponse = await client.CreateOperationRawAsync(
            E2ETelecomOperationSeeds.BuildCreatePayload(recycleSeed),
            ct);
        Assert.NotEqual(System.Net.HttpStatusCode.OK, blockedResponse.StatusCode);

        await E2EAdvancedTestHelper.ReleaseMsisdnFromQuarantineAsync(
            _fixture.AppFactory.Services,
            msisdnAssetId,
            ct);

        var availableStatus = await E2EAdvancedTestHelper.GetMsisdnPoolStatusAsync(
            _fixture.AppFactory.Services,
            msisdnAssetId,
            ct);
        Assert.Equal(MsisdnPoolStatus.Available, availableStatus);

        var kycSeed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.NewActivation,
            "aging-activation-secondary",
            ct);

        var reusePayload = new
        {
            kind = (int)TelecomOperationKind.NewActivation,
            subscriberProfileId = kycSeed.Line.SubscriberProfileId,
            msisdnAssetId,
            productOfferingId = kycSeed.Line.ProductOfferingId,
            simInventoryId = kycSeed.Line.SimInventoryId,
            kycDocumentReferenceId = kycSeed.KycDocumentReferenceId,
            targetSubscriptionTypeId = Domain.Common.TelecomSubscriptionTypeWellKnownIds.Prepaid,
            activationChannel = (int)ActivationChannel.Showroom,
        };

        var reuseOpId = await client.CreateOperationAsync(reusePayload, ct);
        await client.UploadDocumentAsync(reuseOpId, ct);
        var reuseCorrelation = await E2ETelecomOperationSeeds.GetCorrelationIdAsync(
            _fixture.AppFactory.Services,
            reuseOpId,
            ct);
        _ = await client.ConfirmAsync(reuseOpId, ct);
        await E2ETestDataHelper.WaitForOutboxProcessedAsync(
            _fixture.AppFactory.Services,
            reuseCorrelation,
            TimeSpan.FromSeconds(90),
            ct);

        var reuseFinal = await WaitForTerminalStatusAsync(_fixture.AppFactory.Services, reuseOpId, ct);
        Assert.Equal(TelecomOperationStatus.Completed, reuseFinal);

        var secondProfileId = await E2EAdvancedTestHelper.GetSubscriberProfileIdForMsisdnAsync(
            _fixture.AppFactory.Services,
            msisdnAssetId,
            ct);

        Assert.False(string.IsNullOrWhiteSpace(secondProfileId));
        Assert.NotEqual(firstProfileId, secondProfileId);
    }
}
