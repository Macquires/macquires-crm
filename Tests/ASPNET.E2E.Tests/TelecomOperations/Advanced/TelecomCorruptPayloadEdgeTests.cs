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
public sealed class TelecomCorruptPayloadEdgeTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomCorruptPayloadEdgeTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableTheory]
    [InlineData(TelecomOperationKind.NewActivation)]
    [InlineData(TelecomOperationKind.SimSwap)]
    public async Task Operation_CorruptHlrPayload_TriggersSafeFailureAndStructuralTicket(TelecomOperationKind kind)
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
                $"corrupt-hlr-{kind}",
                ct);

            _ = await client.ConfirmAsync(operationId, ct);

            await E2ETestDataHelper.WaitForPendingOutboxAsync(
                _fixture.AppFactory.Services,
                correlationId,
                TimeSpan.FromSeconds(20),
                ct);

            await client.SetSimulatorChaosAsync(
                enabled: true,
                timeoutProbability: 0,
                corruptPayload: true,
                cancellationToken: ct);

            await E2EAdvancedTestHelper.TriggerOutboxDispatchBatchAsync(_fixture.AppFactory.Services, ct);

            var finalStatus = await E2ETestDataHelper.WaitForOperationStatusAsync(
                _fixture.AppFactory.Services,
                operationId,
                s => s is TelecomOperationStatus.Failed
                    or TelecomOperationStatus.ProvisioningError
                    or TelecomOperationStatus.PendingExternal,
                TimeSpan.FromSeconds(90),
                ct);

            Assert.True(
                finalStatus is TelecomOperationStatus.Failed or TelecomOperationStatus.ProvisioningError,
                $"Expected safe failure after corrupt HLR payload, got {finalStatus}");

            using var scope = _fixture.AppFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var hlrLogs = await db.TelecomIntegrationLog.AsNoTracking()
                .Where(l => !l.IsDeleted
                            && l.Msisdn == seed.Line.Msisdn
                            && l.IntegrationSystem == TelecomIntegrationSystem.Huawei_HLR)
                .ToListAsync(ct);
            Assert.NotEmpty(hlrLogs);

            var tickets = await db.TelecomTechnicalTicket.AsNoTracking()
                .Where(t => !t.IsDeleted && t.Msisdn == seed.Line.Msisdn)
                .ToListAsync(ct);
            Assert.NotEmpty(tickets);
            Assert.Contains(tickets, t =>
                t.IssueType == TechnicalTicketIssueType.Provisioning
                || t.Priority == TechnicalTicketPriority.High);
        }
        finally
        {
            await client.ResetSimulatorChaosAsync(CancellationToken.None);
        }
    }
}
