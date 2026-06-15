using Application.Common.Events;
using Application.Common.Telecom;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPNET.E2E.Tests.TelecomOperations;

public abstract class TelecomE2ETestBase
{
    protected static Infrastructure.TelecomE2EClient CreateClient(Infrastructure.TelecomE2EFixture fixture)
    {
        var http = fixture.AppFactory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
        return new Infrastructure.TelecomE2EClient(http, fixture.SimulatorClient);
    }

    protected static async Task<(string OperationId, string CorrelationId, Infrastructure.TelecomOperationScenarioSeed Seed)> CreateReadyOperationAsync(
        Infrastructure.TelecomE2EFixture fixture,
        Infrastructure.TelecomE2EClient client,
        TelecomOperationKind kind,
        string scenarioKey,
        CancellationToken cancellationToken)
    {
        var seed = await Infrastructure.E2ETelecomOperationSeeds.ResolveAsync(
            fixture.AppFactory.Services,
            kind,
            scenarioKey,
            cancellationToken);

        var payload = Infrastructure.E2ETelecomOperationSeeds.BuildCreatePayload(seed);
        var operationId = await client.CreateOperationAsync(payload, cancellationToken);
        await client.UploadDocumentAsync(operationId, cancellationToken);

        if (await Infrastructure.E2ETelecomOperationSeeds.RequiresBackOfficeApprovalAsync(
                fixture.AppFactory.Services,
                operationId,
                cancellationToken))
        {
            await client.ApproveBackOfficeAsync(operationId, cancellationToken);
        }

        var correlationId = await Infrastructure.E2ETelecomOperationSeeds.GetCorrelationIdAsync(
            fixture.AppFactory.Services,
            operationId,
            cancellationToken);

        return (operationId, correlationId, seed);
    }

    protected static async Task<TelecomOperationStatus> WaitForTerminalStatusAsync(
        IServiceProvider services,
        string operationId,
        CancellationToken cancellationToken)
    {
        return await Infrastructure.E2ETestDataHelper.WaitForOperationStatusAsync(
            services,
            operationId,
            s => s is TelecomOperationStatus.Completed
                or TelecomOperationStatus.Failed
                or TelecomOperationStatus.ProvisioningError,
            TimeSpan.FromSeconds(90),
            cancellationToken);
    }

    protected static async Task AssertHappyPathArtifactsAsync(
        Infrastructure.TelecomE2EFixture fixture,
        TelecomOperationKind kind,
        string operationId,
        string correlationId,
        string msisdn,
        CancellationToken cancellationToken)
    {
        using var scope = fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        if (Infrastructure.E2ETelecomOperationSeeds.RequiresNetworkProvision(kind))
        {
            var outbox = await db.IntegrationOutboxMessage.AsNoTracking()
                .Where(m => !m.IsDeleted && m.CorrelationId == correlationId)
                .ToListAsync(cancellationToken);
            Assert.NotEmpty(outbox);
            Assert.All(outbox, m => Assert.NotNull(m.ProcessedAtUtc));
            Assert.Contains(outbox, m => m.EventType == nameof(TelecomOperationProvisionedNotification));

            var integrationLogs = await db.TelecomIntegrationLog.AsNoTracking()
                .Where(l => !l.IsDeleted && l.Msisdn == msisdn)
                .ToListAsync(cancellationToken);
            Assert.Contains(integrationLogs, l =>
                l.IntegrationSystem == TelecomIntegrationSystem.Huawei_CBS && l.IsSuccess);
            Assert.Contains(integrationLogs, l =>
                l.IntegrationSystem == TelecomIntegrationSystem.Huawei_HLR && l.IsSuccess);
        }

        var op = await db.TelecomOperationRequest.AsNoTracking()
            .FirstAsync(o => o.Id == operationId, cancellationToken);
        Assert.Equal(TelecomOperationStatus.Completed, op.Status);
    }
}
