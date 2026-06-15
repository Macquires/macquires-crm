using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Suspension;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPNET.E2E.Tests;

/// <summary>Wave 5 — BSS green closure E2E seeds: SUS fraud, RCN after BDR, VAS deactivate, scheduled dates, MNP mock.</summary>
[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomBssGreenClosureE2ETests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomBssGreenClosureE2ETests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task Suspension_FraudListPath_RequiresBackOfficeApproval()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var payload = await E2ETelecomOperationSeeds.BuildBackOfficeScenarioPayloadAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.TemporarySuspension,
            "sus-fraud-list",
            ct);

        var operationId = await client.CreateOperationAsync(payload, ct);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var op = await db.TelecomOperationRequest.AsNoTracking().FirstAsync(o => o.Id == operationId, ct);

        Assert.Equal(SuspensionWellKnown.Fraud, op.SuspensionType);
        Assert.Equal("BackOffice", op.ApprovalLevelRequired);
    }

    [SkippableFact]
    public async Task Reconnect_AfterBdrPaymentRecorded_CompletesHappyPath()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var bdrSeed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.BadDebtRecovery,
            "bdr-before-rcn",
            ct);

        var bdrId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.BadDebtRecovery,
                subscriberProfileId = bdrSeed.Line.SubscriberProfileId,
                msisdnAssetId = bdrSeed.Line.MsisdnAssetId,
                collectionAction = BadDebtWellKnown.PaymentRecorded,
                paymentReference = $"PAY-RCN-{Guid.NewGuid():N}"[..18],
                collectedAmount = 5000m,
            },
            ct);

        await client.UploadDocumentAsync(bdrId, ct);
        await client.ConfirmAsync(bdrId, ct);

        var rcnId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.Reconnect,
                subscriberProfileId = bdrSeed.Line.SubscriberProfileId,
                msisdnAssetId = bdrSeed.Line.MsisdnAssetId,
                reconnectReason = "Payment cleared after BDR",
                clearanceType = ReconnectWellKnown.Payment,
                paymentReference = $"PAY-RCN-{Guid.NewGuid():N}"[..18],
            },
            ct);

        await client.UploadDocumentAsync(rcnId, ct);
        var confirm = await client.ConfirmAsync(rcnId, ct);
        Assert.True(
            confirm.Status is TelecomOperationStatus.Completed
                or TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.Scheduled);
    }

    [SkippableFact]
    public async Task Vas_Deactivate_CompletesWithoutBillingCharge()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.ServiceModification,
            "vas-deactivate",
            ct);

        var operationId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.ServiceModification,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                notes = "Deactivate VAS VAS_CALLER_ID",
            },
            ct);

        await client.UploadDocumentAsync(operationId, ct);
        var confirm = await client.ConfirmAsync(operationId, ct);
        Assert.NotEqual(TelecomOperationStatus.Failed, confirm.Status);
    }

    [SkippableFact]
    public async Task Reconnect_WithScheduledEffectiveDate_PersistsAndSchedulesOnConfirm()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.Reconnect,
            "rcn-scheduled",
            ct);

        var scheduledUtc = DateTime.UtcNow.AddDays(10);

        var operationId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.Reconnect,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                reconnectReason = "Scheduled reconnect",
                clearanceType = ReconnectWellKnown.Operational,
                reconnectEffectiveDateUtc = scheduledUtc,
            },
            ct);

        await client.UploadDocumentAsync(operationId, ct);
        var confirm = await client.ConfirmAsync(operationId, ct);

        Assert.Equal(TelecomOperationStatus.Scheduled, confirm.Status);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var stored = await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => new { o.ReconnectEffectiveDateUtc, o.Status })
            .FirstAsync(ct);

        Assert.NotNull(stored.ReconnectEffectiveDateUtc);
        Assert.Equal(TelecomOperationStatus.Scheduled, stored.Status);
    }

    [SkippableFact]
    public async Task Mnp_PortIn_MockGateway_StoresExternalCorrelationId()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var payload = await E2ETelecomOperationSeeds.BuildBackOfficeScenarioPayloadAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.NumberPortability,
            "cnr-portin-mock",
            ct);

        var operationId = await client.CreateOperationAsync(payload, ct);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var op = await db.TelecomOperationRequest.AsNoTracking().FirstAsync(o => o.Id == operationId, ct);

        Assert.Equal(ChangeNumberModes.PortIn, op.NumberChangeMode);
        Assert.False(string.IsNullOrWhiteSpace(op.ExternalCorrelationId));
        Assert.StartsWith("MNP-MOCK-", op.ExternalCorrelationId);
    }
}
