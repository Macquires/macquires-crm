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
public sealed class TelecomBackOfficeApprovalPathTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomBackOfficeApprovalPathTests(TelecomE2EFixture fixture) => _fixture = fixture;

    public static TheoryData<TelecomOperationKind, string, TelecomOperationStatus?> BackOfficeScenarios() =>
        new()
        {
            { TelecomOperationKind.TakeOver, "bo-takeover", TelecomOperationStatus.Completed },
            { TelecomOperationKind.NumberPortability, "bo-cnr-premium", TelecomOperationStatus.Completed },
            { TelecomOperationKind.NumberPortability, "bo-cnr-portin", TelecomOperationStatus.Completed },
            { TelecomOperationKind.TemporarySuspension, "bo-sus-fraud", TelecomOperationStatus.Completed },
            { TelecomOperationKind.Termination, "bo-trm-regulatory", TelecomOperationStatus.Completed },
            { TelecomOperationKind.DepositRefundSettlement, "bo-rfd-syriatel", TelecomOperationStatus.Completed },
            { TelecomOperationKind.BadDebtRecovery, "bo-bdr-writeoff", TelecomOperationStatus.Approved_Pending_Cash },
        };

    [SkippableTheory]
    [MemberData(nameof(BackOfficeScenarios))]
    public async Task BackOfficeScenario_RequiresApprovalThenReachesExpectedStatus(
        TelecomOperationKind kind,
        string scenarioKey,
        TelecomOperationStatus? expectedTerminalStatus)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var payload = await E2ETelecomOperationSeeds.BuildBackOfficeScenarioPayloadAsync(
            _fixture.AppFactory.Services,
            kind,
            scenarioKey,
            ct);

        var operationId = await client.CreateOperationAsync(payload, ct);

        Assert.True(
            await E2ETelecomOperationSeeds.RequiresBackOfficeApprovalAsync(
                _fixture.AppFactory.Services,
                operationId,
                ct),
            $"Expected BackOffice approval for {kind} / {scenarioKey}.");

        var confirmBeforeBo = await client.ConfirmRawAsync(operationId, ct);
        Assert.NotEqual(System.Net.HttpStatusCode.OK, confirmBeforeBo.StatusCode);

        await client.UploadDocumentAsync(operationId, ct);
        await client.ApproveBackOfficeAsync(operationId, ct);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var status = await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.Status)
            .FirstAsync(ct);

        if (expectedTerminalStatus == TelecomOperationStatus.Completed
            && E2ETelecomOperationSeeds.RequiresNetworkProvision(kind))
        {
            var correlationId = await E2ETelecomOperationSeeds.GetCorrelationIdAsync(
                _fixture.AppFactory.Services,
                operationId,
                ct);
            await E2ETestDataHelper.WaitForOutboxProcessedAsync(
                _fixture.AppFactory.Services,
                correlationId,
                TimeSpan.FromSeconds(90),
                ct);
            status = await WaitForTerminalStatusAsync(_fixture.AppFactory.Services, operationId, ct);
        }

        Assert.Equal(expectedTerminalStatus, status);
    }
}
