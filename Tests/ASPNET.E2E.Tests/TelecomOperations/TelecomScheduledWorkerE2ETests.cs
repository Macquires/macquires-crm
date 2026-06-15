using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomScheduledWorkerE2ETests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomScheduledWorkerE2ETests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task MigrationScheduled_WhenNotYetDue_WorkflowRejectsExecution()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.Migration,
            "mgr-worker-not-due",
            ct);

        var scheduledUtc = DateTime.UtcNow.AddDays(7);
        var operationId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.Migration,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                productOfferingId = seed.MigrationOfferingId,
                migrationEffectiveDateUtc = scheduledUtc,
            },
            ct);

        await client.UploadDocumentAsync(operationId, ct);
        var confirm = await client.ConfirmAsync(operationId, ct);
        Assert.Equal(TelecomOperationStatus.Scheduled, confirm.Status);

        var result = await E2ETestDataHelper.ExecuteDueScheduledOperationAsync(
            _fixture.AppFactory.Services,
            operationId,
            ct);

        Assert.Equal(TelecomOperationStatus.Scheduled, result.Operation.Status);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var status = await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.Status)
            .FirstAsync(ct);
        Assert.Equal(TelecomOperationStatus.Scheduled, status);
    }

    [SkippableFact]
    public async Task MigrationScheduled_WhenDue_ExecutesAndCompletes()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.Migration,
            "mgr-worker-due",
            ct);

        var operationId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.Migration,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                productOfferingId = seed.MigrationOfferingId,
                migrationEffectiveDateUtc = DateTime.UtcNow.AddDays(14),
            },
            ct);

        await client.UploadDocumentAsync(operationId, ct);
        var confirm = await client.ConfirmAsync(operationId, ct);
        Assert.Equal(TelecomOperationStatus.Scheduled, confirm.Status);

        var correlationId = await E2ETelecomOperationSeeds.GetCorrelationIdAsync(
            _fixture.AppFactory.Services,
            operationId,
            ct);

        await E2ETestDataHelper.BackdateScheduledOperationEffectiveDateAsync(
            _fixture.AppFactory.Services,
            operationId,
            DateTime.UtcNow.AddHours(-1),
            ct);

        var execute = await E2ETestDataHelper.ExecuteDueScheduledOperationAsync(
            _fixture.AppFactory.Services,
            operationId,
            ct);

        Assert.True(
            execute.Operation.Status is TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.Completed
                or TelecomOperationStatus.PendingExternal,
            $"Unexpected status after scheduled execute: {execute.Operation.Status}");

        if (E2ETelecomOperationSeeds.RequiresNetworkProvision(TelecomOperationKind.Migration))
        {
            await E2ETestDataHelper.WaitForOutboxProcessedAsync(
                _fixture.AppFactory.Services,
                correlationId,
                TimeSpan.FromSeconds(90),
                ct);
        }

        var finalStatus = await WaitForTerminalStatusAsync(_fixture.AppFactory.Services, operationId, ct);
        Assert.Equal(TelecomOperationStatus.Completed, finalStatus);

        await AssertHappyPathArtifactsAsync(
            _fixture,
            TelecomOperationKind.Migration,
            operationId,
            correlationId,
            seed.Line.Msisdn,
            ct);
    }
}
