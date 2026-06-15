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
public sealed class TelecomOperationIdempotencyTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomOperationIdempotencyTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableTheory]
    [MemberData(nameof(TelecomOperationDefinitions.AllOperations), MemberType = typeof(TelecomOperationDefinitions))]
    public async Task Operation_ConcurrentConfirm_ReturnsIdempotentReplayWithoutDoubleProvisioning(TelecomOperationKind kind)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var (operationId, correlationId, _) = await CreateReadyOperationAsync(
            _fixture,
            client,
            kind,
            $"idempotency-{TelecomOperationDefinitions.DisplayName(kind)}",
            ct);

        var firstConfirm = await client.ConfirmAsync(operationId, ct);
        Assert.False(firstConfirm.IdempotentReplay);

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

        var concurrent = await Task.WhenAll(
            Enumerable.Range(0, 5)
                .Select(_ => client.ConfirmAsync(operationId, ct)));

        Assert.All(concurrent, r => Assert.True(r.IdempotentReplay));
        Assert.All(concurrent, r => Assert.Equal(TelecomOperationStatus.Completed, r.Status));

        using (var scope = _fixture.AppFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var auditCount = await db.TelecomOperationAuditLog.AsNoTracking()
                .CountAsync(l => !l.IsDeleted && l.TelecomOperationRequestId == operationId, ct);
            Assert.True(auditCount >= 1);

            if (E2ETelecomOperationSeeds.RequiresNetworkProvision(kind))
            {
                var outboxCount = await db.IntegrationOutboxMessage.AsNoTracking()
                    .CountAsync(m => !m.IsDeleted && m.CorrelationId == correlationId, ct);
                Assert.Equal(1, outboxCount);
            }
        }
    }

    [SkippableFact]
    public async Task NewActivation_ConcurrentReserveMsisdn_OnlyOneReservationSucceeds()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.NewActivation,
            "reserve-concurrency",
            ct);

        var attempts = await Task.WhenAll(
            Enumerable.Range(0, 6)
                .Select(async _ =>
                {
                    try
                    {
                        await client.ReserveMsisdnAsync(seed.Line.MsisdnAssetId, seed.Line.CustomerId, ct);
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                }));

        Assert.Equal(1, attempts.Count(x => x));
        Assert.Equal(5, attempts.Count(x => !x));
    }
}
