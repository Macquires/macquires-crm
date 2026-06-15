using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Application.Common.Telecom.Suspension;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomInFlightSuspensionRaceTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomInFlightSuspensionRaceTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task ChangeNumber_InFlightSuspension_EmergencySuspensionWinsAndBlocksModification()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        try
        {
            await client.SetSimulatorChaosAsync(
                enabled: true,
                timeoutProbability: 0,
                responseDelayMs: 4000,
                corruptPayload: false,
                cancellationToken: ct);

            var lineSeed = await E2ETelecomOperationSeeds.ResolveAsync(
                _fixture.AppFactory.Services,
                TelecomOperationKind.NumberPortability,
                "race-change-number",
                ct);

            var changePayload = E2ETelecomOperationSeeds.BuildCreatePayload(lineSeed);
            var changeOpId = await client.CreateOperationAsync(changePayload, ct);
            await client.UploadDocumentAsync(changeOpId, ct);

            var suspensionPayload = new
            {
                kind = (int)TelecomOperationKind.TemporarySuspension,
                subscriberProfileId = lineSeed.Line.SubscriberProfileId,
                msisdnAssetId = lineSeed.Line.MsisdnAssetId,
                suspensionType = SuspensionWellKnown.Fraud,
                suspensionReason = "Stolen SIM emergency",
                barringLevel = "Full",
            };
            var suspensionOpId = await client.CreateOperationAsync(suspensionPayload, ct);
            await client.UploadDocumentAsync(suspensionOpId, ct);
            await client.ApproveBackOfficeAsync(suspensionOpId, ct);

            var changeConfirmTask = client.ConfirmAsync(changeOpId, ct);
            await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            var suspensionConfirm = await client.ConfirmAsync(suspensionOpId, ct);

            Assert.True(
                suspensionConfirm.Status is TelecomOperationStatus.Provisioning
                    or TelecomOperationStatus.Completed
                    or TelecomOperationStatus.PendingExternal,
                $"Suspension confirm unexpected status: {suspensionConfirm.Status}");

            TelecomE2EClient.ConfirmOperationResult changeConfirm;
            try
            {
                changeConfirm = await changeConfirmTask;
            }
            catch (InvalidOperationException)
            {
                changeConfirm = new TelecomE2EClient.ConfirmOperationResult(changeOpId, TelecomOperationStatus.Failed, false);
            }

            var suspensionFinal = await E2ETestDataHelper.WaitForOperationStatusAsync(
                _fixture.AppFactory.Services,
                suspensionOpId,
                s => s is TelecomOperationStatus.Completed
                    or TelecomOperationStatus.Failed
                    or TelecomOperationStatus.ProvisioningError,
                TimeSpan.FromSeconds(90),
                ct);

            Assert.True(
                suspensionFinal is TelecomOperationStatus.Completed or TelecomOperationStatus.Provisioning,
                $"Suspension should win race, got {suspensionFinal}");

            using var scope = _fixture.AppFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var msisdn = await db.MsisdnAsset.AsNoTracking()
                .FirstAsync(m => m.Id == lineSeed.Line.MsisdnAssetId, ct);

            Assert.Equal(MsisdnPoolStatus.Suspended, msisdn.PoolStatus);

            var changeOp = await db.TelecomOperationRequest.AsNoTracking()
                .FirstAsync(o => o.Id == changeOpId, ct);

            Assert.True(
                changeOp.Status is TelecomOperationStatus.Failed
                    or TelecomOperationStatus.ProvisioningError
                    or TelecomOperationStatus.PendingExternal
                    or TelecomOperationStatus.Provisioning,
                $"In-flight change number should not complete cleanly when emergency suspension wins (status={changeOp.Status}).");

            Assert.NotEqual(TelecomOperationStatus.Completed, changeConfirm.Status);
        }
        finally
        {
            await client.ResetSimulatorChaosAsync(CancellationToken.None);
        }
    }
}
