using System.Net;
using Application.Common.Telecom;
using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Domain.Enums;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomFinancialBoundaryEdgeTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomFinancialBoundaryEdgeTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task NewActivation_QuarantinedMsisdn_RejectsBeforeReservationAndReleasesLock()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.NewActivation,
            "financial-quarantine",
            ct);

        await E2EAdvancedTestHelper.QuarantineMsisdnAsync(
            _fixture.AppFactory.Services,
            seed.Line.MsisdnAssetId,
            DateTime.UtcNow.AddDays(90),
            ct);

        var beforeStatus = await E2EAdvancedTestHelper.GetMsisdnPoolStatusAsync(
            _fixture.AppFactory.Services,
            seed.Line.MsisdnAssetId,
            ct);
        Assert.Equal(MsisdnPoolStatus.Quarantined, beforeStatus);

        var payload = E2ETelecomOperationSeeds.BuildCreatePayload(seed);
        var response = await client.CreateOperationRawAsync(payload, ct);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("VAL-02-02", body, StringComparison.OrdinalIgnoreCase);

        var afterStatus = await E2EAdvancedTestHelper.GetMsisdnPoolStatusAsync(
            _fixture.AppFactory.Services,
            seed.Line.MsisdnAssetId,
            ct);
        Assert.Equal(MsisdnPoolStatus.Quarantined, afterStatus);
    }

    [SkippableFact]
    public async Task Migration_DebtBoundaryMinusOneCent_BlocksBeforeDbTransaction()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.Migration,
            "financial-debt-boundary",
            ct);

        if (seed.Line.Msisdn != TelecomDemoMsisdn.DebtSubscriber)
        {
            await E2EAdvancedTestHelper.SetSimulatorBalanceAsync(
                _fixture.SimulatorClient,
                seed.Line.Msisdn,
                -0.01m,
                ct);
        }

        var payload = E2ETelecomOperationSeeds.BuildCreatePayload(seed);
        var response = await client.CreateOperationRawAsync(payload, ct);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.True(
            body.Contains("VAL-11-12", StringComparison.OrdinalIgnoreCase)
            || body.Contains("رصيد مستحق", StringComparison.OrdinalIgnoreCase),
            $"Expected debt boundary rejection, got: {body}");

        var poolStatus = await E2EAdvancedTestHelper.GetMsisdnPoolStatusAsync(
            _fixture.AppFactory.Services,
            seed.Line.MsisdnAssetId,
            ct);
        Assert.Equal(MsisdnPoolStatus.Active, poolStatus);
    }
}
