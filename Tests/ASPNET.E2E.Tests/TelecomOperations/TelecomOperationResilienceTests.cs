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
public sealed class TelecomOperationResilienceTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomOperationResilienceTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableTheory]
    [MemberData(nameof(TelecomOperationDefinitions.NetworkOperations), MemberType = typeof(TelecomOperationDefinitions))]
    public async Task Operation_HlrTimeout_TriggersCompensationAndTechnicalTicket(TelecomOperationKind kind)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        try
        {
            var (operationId, correlationId, seed) = await CreateReadyOperationAsync(
                _fixture,
                client,
                kind,
                $"resilience-alt-{TelecomOperationDefinitions.DisplayName(kind)}",
                ct);

            _ = await client.ConfirmAsync(operationId, ct);

            await E2ETestDataHelper.WaitForPendingOutboxAsync(
                _fixture.AppFactory.Services,
                correlationId,
                TimeSpan.FromSeconds(15),
                ct);

            await client.SetSimulatorChaosAsync(enabled: true, timeoutProbability: 100, ct);

            await E2ETestDataHelper.WaitForOutboxProcessedAsync(
                _fixture.AppFactory.Services,
                correlationId,
                TimeSpan.FromSeconds(60),
                ct);

            var finalStatus = await E2ETestDataHelper.WaitForOperationStatusAsync(
                _fixture.AppFactory.Services,
                operationId,
                s => s is TelecomOperationStatus.Failed
                    or TelecomOperationStatus.ProvisioningError
                    or TelecomOperationStatus.PendingExternal,
                TimeSpan.FromSeconds(60),
                ct);

            Assert.True(
                finalStatus is TelecomOperationStatus.Failed or TelecomOperationStatus.ProvisioningError,
                $"Expected failure compensation for {kind}, got {finalStatus}");

            using var scope = _fixture.AppFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var hlrLogs = await db.TelecomIntegrationLog.AsNoTracking()
                .Where(l => !l.IsDeleted
                            && l.Msisdn == seed.Line.Msisdn
                            && l.IntegrationSystem == TelecomIntegrationSystem.Huawei_HLR)
                .ToListAsync(ct);
            Assert.Contains(hlrLogs, l => !l.IsSuccess);

            var tickets = await db.TelecomTechnicalTicket.AsNoTracking()
                .Where(t => !t.IsDeleted && t.Msisdn == seed.Line.Msisdn)
                .ToListAsync(ct);
            Assert.NotEmpty(tickets);
            Assert.Contains(tickets, t =>
                t.IssueType == TechnicalTicketIssueType.Provisioning
                || (t.PayloadJson != null && t.PayloadJson.Contains("fallout", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            await CreateClient(_fixture).SetSimulatorChaosAsync(enabled: false, timeoutProbability: 0, ct);
        }
    }

    [SkippableTheory]
    [MemberData(nameof(TelecomOperationDefinitions.NonNetworkOperations), MemberType = typeof(TelecomOperationDefinitions))]
    public async Task Operation_NonNetworkKind_CompletesWithoutHlrOutbox(TelecomOperationKind kind)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var (operationId, correlationId, _) = await CreateReadyOperationAsync(
            _fixture,
            client,
            kind,
            $"resilience-local-{TelecomOperationDefinitions.DisplayName(kind)}",
            ct);

        _ = await client.ConfirmAsync(operationId, ct);
        var finalStatus = await WaitForTerminalStatusAsync(_fixture.AppFactory.Services, operationId, ct);
        Assert.Equal(TelecomOperationStatus.Completed, finalStatus);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var outbox = await db.IntegrationOutboxMessage.AsNoTracking()
            .AnyAsync(m => !m.IsDeleted && m.CorrelationId == correlationId, ct);
        Assert.False(outbox);
    }
}
