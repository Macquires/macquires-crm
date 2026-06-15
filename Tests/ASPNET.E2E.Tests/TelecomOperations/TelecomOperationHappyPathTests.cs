using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Domain.Enums;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomOperationHappyPathTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomOperationHappyPathTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableTheory]
    [MemberData(nameof(TelecomOperationDefinitions.AllOperations), MemberType = typeof(TelecomOperationDefinitions))]
    public async Task Operation_HappyPath_CompletesWithExpectedArtifacts(TelecomOperationKind kind)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var (operationId, correlationId, seed) = await CreateReadyOperationAsync(
            _fixture,
            client,
            kind,
            $"happy-{TelecomOperationDefinitions.DisplayName(kind)}",
            ct);

        var confirm = await client.ConfirmAsync(operationId, ct);
        Assert.True(
            confirm.Status is TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.Completed
                or TelecomOperationStatus.PendingExternal,
            $"Unexpected immediate confirm status for {kind}: {confirm.Status}");

        if (E2ETelecomOperationSeeds.RequiresNetworkProvision(kind))
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
            kind,
            operationId,
            correlationId,
            seed.Line.Msisdn,
            ct);
    }
}
