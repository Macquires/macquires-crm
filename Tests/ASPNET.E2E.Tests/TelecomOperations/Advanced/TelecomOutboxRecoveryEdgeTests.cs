using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Domain.Enums;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomOutboxRecoveryEdgeTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomOutboxRecoveryEdgeTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task NewActivation_OutboxPendingAfterCommit_RecoversExactlyOnceOnNextDispatch()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.NewActivation,
            "outbox-split-brain",
            ct);

        var payload = E2ETelecomOperationSeeds.BuildCreatePayload(seed);
        var operationId = await client.CreateOperationAsync(payload, ct);
        await client.UploadDocumentAsync(operationId, ct);

        var correlationId = await E2ETelecomOperationSeeds.GetCorrelationIdAsync(
            _fixture.AppFactory.Services,
            operationId,
            ct);

        _ = await client.ConfirmAsync(operationId, ct);

        await E2ETestDataHelper.WaitForPendingOutboxAsync(
            _fixture.AppFactory.Services,
            correlationId,
            TimeSpan.FromSeconds(30),
            ct);

        var pendingSnapshot = await E2EAdvancedTestHelper.GetOutboxSnapshotAsync(
            _fixture.AppFactory.Services,
            correlationId,
            ct);
        Assert.Equal(1, pendingSnapshot.TotalMessages);
        Assert.Equal(0, pendingSnapshot.ProcessedCount);

        await E2EAdvancedTestHelper.TriggerOutboxDispatchBatchAsync(_fixture.AppFactory.Services, ct);

        var afterFirstDispatch = await E2EAdvancedTestHelper.GetOutboxSnapshotAsync(
            _fixture.AppFactory.Services,
            correlationId,
            ct);
        Assert.Equal(1, afterFirstDispatch.TotalMessages);
        Assert.Equal(1, afterFirstDispatch.ProcessedCount);

        await E2EAdvancedTestHelper.TriggerOutboxDispatchBatchAsync(_fixture.AppFactory.Services, ct);

        var afterSecondDispatch = await E2EAdvancedTestHelper.GetOutboxSnapshotAsync(
            _fixture.AppFactory.Services,
            correlationId,
            ct);
        Assert.Equal(1, afterSecondDispatch.TotalMessages);
        Assert.Equal(1, afterSecondDispatch.ProcessedCount);

        var finalStatus = await E2ETestDataHelper.WaitForOperationStatusAsync(
            _fixture.AppFactory.Services,
            operationId,
            s => s is TelecomOperationStatus.Completed or TelecomOperationStatus.Failed,
            TimeSpan.FromSeconds(90),
            ct);

        Assert.Equal(TelecomOperationStatus.Completed, finalStatus);

        var hlrStatus = await _fixture.SimulatorClient.GetAsync(
            $"/hlr/subscribers/{seed.Line.Msisdn}/status",
            ct);
        hlrStatus.EnsureSuccessStatusCode();
        var hlrBody = await hlrStatus.Content.ReadAsStringAsync(ct);
        Assert.Contains("ACTIVE", hlrBody, StringComparison.OrdinalIgnoreCase);
    }
}
