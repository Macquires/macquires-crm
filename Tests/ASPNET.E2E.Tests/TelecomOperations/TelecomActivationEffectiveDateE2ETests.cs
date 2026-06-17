using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Application.Common.Telecom;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomActivationEffectiveDateE2ETests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomActivationEffectiveDateE2ETests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task ActivationCreate_WithScheduledEffectiveDate_PersistsOnOperation()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETestDataHelper.ResolveActivationSeedAsync(_fixture.AppFactory.Services, ct);
        var scheduledUtc = DateTime.UtcNow.Date.AddDays(7);
        var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(seed.Msisdn)
            ?? throw new InvalidOperationException("Could not derive ICCID for activation seed.");

        await client.ReserveMsisdnAsync(seed.MsisdnAssetId, seed.CustomerId, ct);

        var operationId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.NewActivation,
                subscriberProfileId = seed.SubscriberProfileId,
                msisdnAssetId = seed.MsisdnAssetId,
                productOfferingId = seed.ProductOfferingId,
                simIccid = iccid,
                kycDocumentReferenceId = seed.KycDocumentReferenceId,
                activationEffectiveDateUtc = scheduledUtc,
            },
            ct);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var stored = await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.ActivationEffectiveDateUtc)
            .FirstAsync(ct);

        Assert.NotNull(stored);
        Assert.Equal(scheduledUtc, stored.Value, precision: TimeSpan.FromSeconds(1));
    }

    [SkippableFact]
    public async Task ActivationConfirm_WithFutureEffectiveDate_SchedulesWithoutProvisioning()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETestDataHelper.ResolveActivationSeedAsync(_fixture.AppFactory.Services, ct);
        var scheduledUtc = DateTime.UtcNow.AddDays(14);
        var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(seed.Msisdn)
            ?? throw new InvalidOperationException("Could not derive ICCID for activation seed.");

        await client.ReserveMsisdnAsync(seed.MsisdnAssetId, seed.CustomerId, ct);

        var operationId = await client.CreateOperationAsync(
            new
            {
                kind = (int)TelecomOperationKind.NewActivation,
                subscriberProfileId = seed.SubscriberProfileId,
                msisdnAssetId = seed.MsisdnAssetId,
                productOfferingId = seed.ProductOfferingId,
                simIccid = iccid,
                kycDocumentReferenceId = seed.KycDocumentReferenceId,
                activationEffectiveDateUtc = scheduledUtc,
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
