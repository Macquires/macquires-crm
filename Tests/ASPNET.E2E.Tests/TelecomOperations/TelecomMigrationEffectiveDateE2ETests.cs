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
public sealed class TelecomMigrationEffectiveDateE2ETests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomMigrationEffectiveDateE2ETests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task MigrationCreate_WithScheduledEffectiveDate_PersistsOnOperation()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.Migration,
            "mgr-effective-date",
            ct);

        var scheduledUtc = DateTime.UtcNow.Date.AddDays(14);

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

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var stored = await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.MigrationEffectiveDateUtc)
            .FirstAsync(ct);

        Assert.NotNull(stored);
        Assert.Equal(scheduledUtc, stored.Value, precision: TimeSpan.FromSeconds(1));
    }

    [SkippableFact]
    public async Task MigrationConfirm_WithFutureEffectiveDate_SchedulesWithoutProvisioning()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.Migration,
            "mgr-schedule-confirm",
            ct);

        var scheduledUtc = DateTime.UtcNow.AddDays(14);

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

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var status = await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.Status)
            .FirstAsync(ct);
        Assert.Equal(TelecomOperationStatus.Scheduled, status);

        var detail = await client.GetOperationDetailAsync(operationId, ct);
        Assert.Equal(TelecomOperationStatus.Scheduled, detail.Status);
        Assert.NotNull(detail.ScheduledEffectiveDateUtc);
        Assert.Equal(scheduledUtc, detail.ScheduledEffectiveDateUtc!.Value, precision: TimeSpan.FromSeconds(1));
    }
}
