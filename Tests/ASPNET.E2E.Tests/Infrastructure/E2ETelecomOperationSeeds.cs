using System.Text;
using Application.Common.CQS.Queries;
using Application.Common.Telecom;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Refund;
using Application.Common.Telecom.Termination;
using Application.Common.Telecom.Suspension;
using Domain.Common;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASPNET.E2E.Tests.Infrastructure;

public sealed record TelecomLineSeed(
    string SubscriberProfileId,
    string CustomerId,
    string MsisdnAssetId,
    string Msisdn,
    string? SimInventoryId,
    string? ProductOfferingId,
    string? ProductId);

public sealed record TelecomOperationScenarioSeed(
    TelecomOperationKind Kind,
    string ScenarioKey,
    TelecomLineSeed Line,
    string? SecondarySubscriberProfileId = null,
    string? TargetMsisdnAssetId = null,
    string? TargetMsisdn = null,
    string? ReplacementSimInventoryId = null,
    string? ReplacementSimIccid = null,
    string? DeviceInventoryId = null,
    string? KycDocumentReferenceId = null,
    string? MigrationOfferingId = null);

public static class E2ETelecomOperationSeeds
{
    private static int _poolOffset;

    public static async Task<TelecomOperationScenarioSeed> ResolveAsync(
        IServiceProvider services,
        TelecomOperationKind kind,
        string scenarioKey,
        CancellationToken cancellationToken = default)
    {
        return kind switch
        {
            TelecomOperationKind.NewActivation => await ResolveNewActivationAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.Migration => await ResolveMigrationAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.TakeOver => await ResolveTakeOverAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.SimSwap => await ResolveSimSwapAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.ServiceModification => await ResolveServiceModificationAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.NumberPortability => await ResolveChangeNumberAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.ChangeGsmType => await ResolveChangeGsmAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.Termination => await ResolveTerminationAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.TemporarySuspension => await ResolveSuspensionAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.Reconnect => await ResolveReconnectAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.DeviceSale => await ResolveDeviceSaleAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.DepositRefundSettlement => await ResolveRefundAsync(services, scenarioKey, cancellationToken),
            TelecomOperationKind.BadDebtRecovery => await ResolveBadDebtAsync(services, scenarioKey, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported telecom kind {kind}.")
        };
    }

    public static object BuildCreatePayload(TelecomOperationScenarioSeed seed)
    {
        return seed.Kind switch
        {
            TelecomOperationKind.NewActivation => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                productOfferingId = seed.Line.ProductOfferingId,
                simInventoryId = seed.Line.SimInventoryId,
                kycDocumentReferenceId = seed.KycDocumentReferenceId,
                targetSubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
                activationChannel = (int)ActivationChannel.Showroom,
            },
            TelecomOperationKind.Migration => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                productOfferingId = seed.MigrationOfferingId,
            },
            TelecomOperationKind.TakeOver => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                secondarySubscriberProfileId = seed.SecondarySubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                transferReason = "Sale",
            },
            TelecomOperationKind.SimSwap => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                replacementReason = "Damaged",
                simInventoryId = seed.ReplacementSimInventoryId,
                simIccid = seed.ReplacementSimIccid,
                isLostOrStolenReport = false,
            },
            TelecomOperationKind.ServiceModification => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                notes = "Activate VAS VAS_CALLER_ID",
            },
            TelecomOperationKind.NumberPortability => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                targetMsisdnAssetId = seed.TargetMsisdnAssetId,
                numberChangeReason = "Personal",
            },
            TelecomOperationKind.ChangeGsmType => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                targetSubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Postpaid,
                gsmMigrationReason = "Upgrade",
            },
            TelecomOperationKind.Termination => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                terminationType = TerminationWellKnown.Voluntary,
                terminationReason = "Leaving",
                retentionOfferOutcome = "Declined",
            },
            TelecomOperationKind.TemporarySuspension => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                suspensionType = SuspensionWellKnown.CustomerRequest,
                suspensionReason = "Travel",
                barringLevel = "Full",
            },
            TelecomOperationKind.Reconnect => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                reconnectReason = "Payment cleared",
                clearanceType = ReconnectWellKnown.Operational,
            },
            TelecomOperationKind.DeviceSale => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                deviceInventoryId = seed.DeviceInventoryId,
                deviceSaleType = (int)DeviceSaleType.Cash,
            },
            TelecomOperationKind.DepositRefundSettlement => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                refundType = RefundWellKnown.TypeDeposit,
                refundMethod = RefundWellKnown.MethodCash,
                refundReason = "Termination",
                refundAmount = 5000m,
            },
            TelecomOperationKind.BadDebtRecovery => new
            {
                kind = (int)seed.Kind,
                subscriberProfileId = seed.Line.SubscriberProfileId,
                msisdnAssetId = seed.Line.MsisdnAssetId,
                collectionAction = BadDebtWellKnown.PaymentRecorded,
                paymentReference = $"PAY-E2E-{Guid.NewGuid():N}"[..20],
                collectedAmount = 5000m,
            },
            _ => throw new NotSupportedException($"Unsupported kind {seed.Kind}.")
        };
    }

    public static bool RequiresNetworkProvision(TelecomOperationKind kind) =>
        kind is TelecomOperationKind.NewActivation
            or TelecomOperationKind.SimSwap
            or TelecomOperationKind.Migration
            or TelecomOperationKind.TakeOver
            or TelecomOperationKind.ChangeGsmType
            or TelecomOperationKind.NumberPortability
            or TelecomOperationKind.Termination
            or TelecomOperationKind.TemporarySuspension
            or TelecomOperationKind.Reconnect;

    public static bool RequiresBackOfficeApproval(TelecomOperationKind kind) =>
        kind == TelecomOperationKind.TakeOver;

    public static async Task<bool> RequiresBackOfficeApprovalAsync(
        IServiceProvider services,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var approval = await query.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Id == operationId)
            .Select(o => o.ApprovalLevelRequired)
            .FirstOrDefaultAsync(cancellationToken);
        return string.Equals(approval, "BackOffice", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<object> BuildBackOfficeScenarioPayloadAsync(
        IServiceProvider services,
        TelecomOperationKind kind,
        string scenarioKey,
        CancellationToken cancellationToken = default)
    {
        var seed = await ResolveAsync(services, kind, scenarioKey, cancellationToken);
        return kind switch
        {
            TelecomOperationKind.NumberPortability when scenarioKey.Contains("portin", StringComparison.OrdinalIgnoreCase) =>
                new
                {
                    kind = (int)kind,
                    subscriberProfileId = seed.Line.SubscriberProfileId,
                    msisdnAssetId = seed.Line.MsisdnAssetId,
                    numberChangeMode = ChangeNumberModes.PortIn,
                    portInMsisdn = "0937600099",
                    donorOperatorCode = "MTN",
                    agencyReference = "MNP-E2E-00001",
                    numberChangeReason = "PortIn",
                },
            TelecomOperationKind.NumberPortability when scenarioKey.Contains("premium", StringComparison.OrdinalIgnoreCase) =>
                new
                {
                    kind = (int)kind,
                    subscriberProfileId = seed.Line.SubscriberProfileId,
                    msisdnAssetId = seed.Line.MsisdnAssetId,
                    targetMsisdnAssetId = await ResolvePremiumPoolMsisdnIdAsync(services, cancellationToken),
                    numberChangeReason = "Premium vanity",
                },
            TelecomOperationKind.TemporarySuspension when scenarioKey.Contains("fraud", StringComparison.OrdinalIgnoreCase) =>
                new
                {
                    kind = (int)kind,
                    subscriberProfileId = seed.Line.SubscriberProfileId,
                    msisdnAssetId = seed.Line.MsisdnAssetId,
                    suspensionType = SuspensionWellKnown.Fraud,
                    suspensionReason = "Suspected fraud — E2E",
                    barringLevel = SuspensionWellKnown.BarringFull,
                    fraudClearanceConfirmed = true,
                },
            TelecomOperationKind.Termination when scenarioKey.Contains("regulatory", StringComparison.OrdinalIgnoreCase) =>
                new
                {
                    kind = (int)kind,
                    subscriberProfileId = seed.Line.SubscriberProfileId,
                    msisdnAssetId = seed.Line.MsisdnAssetId,
                    terminationType = TerminationWellKnown.Regulatory,
                    terminationReason = "Regulatory order — E2E",
                },
            TelecomOperationKind.DepositRefundSettlement when scenarioKey.Contains("syriatel", StringComparison.OrdinalIgnoreCase) =>
                new
                {
                    kind = (int)kind,
                    subscriberProfileId = seed.Line.SubscriberProfileId,
                    msisdnAssetId = seed.Line.MsisdnAssetId,
                    refundType = RefundWellKnown.TypeSyriatelCash,
                    refundMethod = RefundWellKnown.MethodWalletCredit,
                    refundReason = "SyriatelCash settlement — E2E",
                    refundAmount = 1000m,
                },
            TelecomOperationKind.BadDebtRecovery when scenarioKey.Contains("writeoff", StringComparison.OrdinalIgnoreCase) =>
                new
                {
                    kind = (int)kind,
                    subscriberProfileId = seed.Line.SubscriberProfileId,
                    msisdnAssetId = seed.Line.MsisdnAssetId,
                    collectionAction = BadDebtWellKnown.WriteOffFull,
                    dunningStage = BadDebtWellKnown.WriteOffPending,
                    writeOffAmount = 5000m,
                    collectionApprovalConfirmed = true,
                },
            _ => BuildCreatePayload(seed),
        };
    }

    private static async Task<string> ResolvePremiumPoolMsisdnIdAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var id = await query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted
                        && m.SubscriberProfileId == null
                        && m.PoolStatus == MsisdnPoolStatus.Available
                        && m.Category != MsisdnCategory.Normal)
            .OrderBy(m => m.Msisdn)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(id))
        {
            var fallback = await ResolveAvailablePoolMsisdnAsync(services, cancellationToken);
            return fallback.AssetId;
        }

        return id;
    }

    private static async Task<TelecomOperationScenarioSeed> ResolveNewActivationAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken)
    {
        var bundle = scenarioKey.Contains("alt", StringComparison.OrdinalIgnoreCase)
            ? await E2ETestDataHelper.ResolveAlternateActivationSeedAsync(services, "", cancellationToken)
            : await E2ETestDataHelper.ResolveActivationSeedAsync(services, cancellationToken);

        return new TelecomOperationScenarioSeed(
            TelecomOperationKind.NewActivation,
            scenarioKey,
            new TelecomLineSeed(
                bundle.SubscriberProfileId,
                bundle.CustomerId,
                bundle.MsisdnAssetId,
                bundle.Msisdn,
                bundle.SimInventoryId,
                bundle.ProductOfferingId,
                null),
            KycDocumentReferenceId: bundle.KycDocumentReferenceId);
    }

    private static async Task<TelecomLineSeed> ResolveActiveLineByMsisdnAsync(
        IServiceProvider services,
        string msisdn,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();

        var row = await query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Msisdn == msisdn && m.PoolStatus == MsisdnPoolStatus.Active)
            .Select(m => new
            {
                m.Id,
                m.Msisdn,
                m.SubscriberProfileId,
                m.ProductId,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Active demo line {msisdn} not found.");

        var profile = await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == row.SubscriberProfileId)
            .Select(p => new { p.Id, p.CustomerId })
            .FirstAsync(cancellationToken);

        var simId = await query.SimInventory.AsNoTracking()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == profile.Id && s.Status == SimStatus.Active)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var offeringId = await query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "YA_HALA_30")
            .Select(o => o.Id)
            .FirstAsync(cancellationToken);

        return new TelecomLineSeed(
            profile.Id,
            profile.CustomerId,
            row.Id,
            row.Msisdn,
            simId,
            offeringId,
            row.ProductId);
    }

    private static async Task<(string AssetId, string Msisdn)> ResolveAvailablePoolMsisdnAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var offset = Interlocked.Increment(ref _poolOffset);

        var row = await query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted
                        && m.SubscriberProfileId == null
                        && m.PoolStatus == MsisdnPoolStatus.Available
                        && m.Msisdn.StartsWith("093555"))
            .OrderBy(m => m.Msisdn)
            .Skip(offset % 20)
            .Select(m => new { m.Id, m.Msisdn })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No available pool MSISDN for E2E.");

        return (row.Id, row.Msisdn);
    }

    private static async Task<(string SimId, string? Iccid)> ResolveReplacementSimAsync(
        IServiceProvider services,
        string excludeProfileId,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();

        var sim = await query.SimInventory.AsNoTracking()
            .Where(s => !s.IsDeleted
                        && s.Status == SimStatus.Available
                        && (s.SubscriberProfileId == null || s.SubscriberProfileId != excludeProfileId))
            .OrderByDescending(s => s.Iccid)
            .Select(s => new { s.Id, s.Iccid })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No replacement SIM available.");

        return (sim.Id, sim.Iccid);
    }

    private static async Task<string> ResolveMigrationOfferingIdAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        return await query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "YA_HALA_PLUS")
            .Select(o => o.Id)
            .FirstAsync(cancellationToken);
    }

    private static async Task<string> ResolveSecondaryProfileIdAsync(
        IServiceProvider services,
        string excludeProfileId,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();

        return await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id != excludeProfileId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => p.Id)
            .FirstAsync(cancellationToken);
    }

    private static async Task<string> ResolveAvailableDeviceIdAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();

        return await query.DeviceInventory.AsNoTracking()
            .Where(d => !d.IsDeleted && d.Status == DeviceInventoryStatus.Available)
            .OrderBy(d => d.Sku)
            .Select(d => d.Id)
            .FirstAsync(cancellationToken);
    }

    private static Task<TelecomOperationScenarioSeed> ResolveMigrationAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.Migration,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseHealthy,
            async (sp, ct) =>
            {
                var offeringId = await ResolveMigrationOfferingIdAsync(sp, ct);
                return (offeringId, (string?)null, (string?)null, (string?)null, (string?)null, (string?)null);
            },
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveTakeOverAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.TakeOver,
            scenarioKey,
            TelecomDemoMsisdn.Hero,
            async (sp, line, ct) =>
            {
                var secondary = await ResolveSecondaryProfileIdAsync(sp, line.SubscriberProfileId, ct);
                return ((string?)null, (string?)null, (string?)null, (string?)null, (string?)null, secondary);
            },
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveSimSwapAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.SimSwap,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseNotProvisioned,
            async (sp, line, ct) =>
            {
                var (simId, iccid) = await ResolveReplacementSimAsync(sp, line.SubscriberProfileId, ct);
                return ((string?)null, (string?)null, simId, iccid, (string?)null, (string?)null);
            },
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveServiceModificationAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.ServiceModification,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseHealthy,
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveChangeNumberAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.NumberPortability,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseHealthy,
            async (sp, _, ct) =>
            {
                var (targetId, _) = await ResolveAvailablePoolMsisdnAsync(sp, ct);
                return ((string?)null, targetId, (string?)null, (string?)null, (string?)null, (string?)null);
            },
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveChangeGsmAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.ChangeGsmType,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseNotProvisioned,
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveTerminationAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.Termination,
            scenarioKey,
            PickMsisdnForScenario(scenarioKey, TelecomDemoMsisdn.ShowcaseHealthy, "0935551001"),
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveSuspensionAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.TemporarySuspension,
            scenarioKey,
            PickMsisdnForScenario(scenarioKey, TelecomDemoMsisdn.ShowcaseHealthy, "0935551002"),
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveReconnectAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.Reconnect,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseOperationalSuspended,
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveDeviceSaleAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.DeviceSale,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseHealthy,
            async (sp, _, ct) =>
            {
                var deviceId = await ResolveAvailableDeviceIdAsync(sp, ct);
                return ((string?)null, (string?)null, (string?)null, (string?)null, deviceId, (string?)null);
            },
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveRefundAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.DepositRefundSettlement,
            scenarioKey,
            TelecomDemoMsisdn.ShowcaseHealthy,
            cancellationToken);

    private static Task<TelecomOperationScenarioSeed> ResolveBadDebtAsync(
        IServiceProvider services,
        string scenarioKey,
        CancellationToken cancellationToken) =>
        ResolveWithLineAsync(
            services,
            TelecomOperationKind.BadDebtRecovery,
            scenarioKey,
            TelecomDemoMsisdn.DebtSubscriber,
            cancellationToken);

    private static string PickMsisdnForScenario(string scenarioKey, string primary, string alternate) =>
        scenarioKey.Contains("alt", StringComparison.OrdinalIgnoreCase) ? alternate : primary;

    private static async Task<TelecomOperationScenarioSeed> ResolveWithLineAsync(
        IServiceProvider services,
        TelecomOperationKind kind,
        string scenarioKey,
        string msisdn,
        CancellationToken cancellationToken) =>
        await ResolveWithLineAsync(
            services,
            kind,
            scenarioKey,
            msisdn,
            static (_, _, _) => Task.FromResult(((string?)null, (string?)null, (string?)null, (string?)null, (string?)null, (string?)null)),
            cancellationToken);

    private static async Task<TelecomOperationScenarioSeed> ResolveWithLineAsync(
        IServiceProvider services,
        TelecomOperationKind kind,
        string scenarioKey,
        string msisdn,
        Func<IServiceProvider, TelecomLineSeed, CancellationToken, Task<(string? MigrationOfferingId, string? TargetMsisdnAssetId, string? ReplacementSimInventoryId, string? ReplacementSimIccid, string? DeviceInventoryId, string? SecondaryProfileId)>> enrich,
        CancellationToken cancellationToken)
    {
        TelecomLineSeed line;
        try
        {
            line = await ResolveActiveLineByMsisdnAsync(services, msisdn, cancellationToken);
        }
        catch (InvalidOperationException) when (msisdn.StartsWith("093555"))
        {
            line = await ResolveActiveLineByMsisdnAsync(services, TelecomDemoMsisdn.ShowcaseHealthy, cancellationToken);
        }

        var extra = await enrich(services, line, cancellationToken);

        return new TelecomOperationScenarioSeed(
            kind,
            scenarioKey,
            line,
            SecondarySubscriberProfileId: extra.SecondaryProfileId,
            TargetMsisdnAssetId: extra.TargetMsisdnAssetId,
            ReplacementSimInventoryId: extra.ReplacementSimInventoryId,
            ReplacementSimIccid: extra.ReplacementSimIccid,
            DeviceInventoryId: extra.DeviceInventoryId,
            MigrationOfferingId: extra.MigrationOfferingId);
    }

    private static async Task<TelecomOperationScenarioSeed> ResolveWithLineAsync(
        IServiceProvider services,
        TelecomOperationKind kind,
        string scenarioKey,
        string msisdn,
        Func<IServiceProvider, CancellationToken, Task<(string? MigrationOfferingId, string? TargetMsisdnAssetId, string? ReplacementSimInventoryId, string? ReplacementSimIccid, string? DeviceInventoryId, string? SecondaryProfileId)>> enrich,
        CancellationToken cancellationToken)
    {
        var line = await ResolveActiveLineByMsisdnAsync(services, msisdn, cancellationToken);
        var extra = await enrich(services, cancellationToken);

        return new TelecomOperationScenarioSeed(
            kind,
            scenarioKey,
            line,
            SecondarySubscriberProfileId: extra.SecondaryProfileId,
            TargetMsisdnAssetId: extra.TargetMsisdnAssetId,
            ReplacementSimInventoryId: extra.ReplacementSimInventoryId,
            ReplacementSimIccid: extra.ReplacementSimIccid,
            DeviceInventoryId: extra.DeviceInventoryId,
            MigrationOfferingId: extra.MigrationOfferingId);
    }

    public static async Task<string> GetCorrelationIdAsync(
        IServiceProvider services,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => o.Id == operationId)
            .Select(o => o.CorrelationId)
            .FirstAsync(cancellationToken)
            ?? throw new InvalidOperationException("CorrelationId missing.");
    }

    public static async Task TagOperationBranchAsync(
        IServiceProvider services,
        string operationId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var op = await db.TelecomOperationRequest.FirstAsync(o => o.Id == operationId, cancellationToken);
        op.BranchId = branchId;
        await db.SaveChangesAsync(cancellationToken);
    }
}
