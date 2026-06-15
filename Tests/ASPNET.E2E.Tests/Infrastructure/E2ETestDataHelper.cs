using System.Text;
using Application.Common.CQS.Queries;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASPNET.E2E.Tests.Infrastructure;

public sealed record ActivationSeedBundle(
    string SubscriberProfileId,
    string CustomerId,
    string MsisdnAssetId,
    string Msisdn,
    string? SimInventoryId,
    string ProductOfferingId,
    string KycDocumentReferenceId);

public static class E2ETestDataHelper
{
    public static async Task<ActivationSeedBundle> ResolveActivationSeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var kyc = scope.ServiceProvider.GetRequiredService<IKycDocumentStorageService>();

        var offering = await query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "YA_HALA_30")
            .Select(o => new { o.Id })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Demo offering YA_HALA_30 not found after seed.");

        var msisdnAsset = await query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted
                        && m.SubscriberProfileId == null
                        && m.PoolStatus == MsisdnPoolStatus.Available
                        && m.Msisdn.StartsWith("093555"))
            .OrderBy(m => m.Msisdn)
            .Select(m => new { m.Id, m.Msisdn })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No available demo MSISDN (093555*) in pool.");

        var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(msisdnAsset.Msisdn);
        string? simId = null;
        if (!string.IsNullOrEmpty(iccid))
        {
            simId = await query.SimInventory.AsNoTracking()
                .Where(s => !s.IsDeleted && s.Iccid == iccid && s.Status == SimStatus.Available)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var profile = await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => new { p.Id, p.CustomerId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No subscriber profile found after demo seed.");

        await using var kycStream = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 E2E-KYC-PLACEHOLDER"));
        var kycRef = await kyc.StoreKycDocumentAsync(
            msisdnAsset.Msisdn,
            "e2e-kyc.pdf",
            kycStream,
            "application/pdf",
            cancellationToken);

        return new ActivationSeedBundle(
            profile.Id,
            profile.CustomerId,
            msisdnAsset.Id,
            msisdnAsset.Msisdn,
            simId,
            offering.Id,
            kycRef);
    }

    public static async Task<string> ResolveDemoCustomerIdAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        return await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => p.CustomerId)
            .FirstAsync(cancellationToken);
    }

    public static async Task<ActivationSeedBundle> ResolveAlternateActivationSeedAsync(
        IServiceProvider services,
        string excludeMsisdnAssetId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var kyc = scope.ServiceProvider.GetRequiredService<IKycDocumentStorageService>();

        var offering = await query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "YA_HALA_30")
            .Select(o => o.Id)
            .FirstAsync(cancellationToken);

        var msisdnAsset = await query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted
                        && m.Id != excludeMsisdnAssetId
                        && m.SubscriberProfileId == null
                        && m.PoolStatus == MsisdnPoolStatus.Available
                        && m.Msisdn.StartsWith("093555"))
            .OrderByDescending(m => m.Msisdn)
            .Select(m => new { m.Id, m.Msisdn })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No alternate demo MSISDN available.");

        var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(msisdnAsset.Msisdn);
        string? simId = null;
        if (!string.IsNullOrEmpty(iccid))
        {
            simId = await query.SimInventory.AsNoTracking()
                .Where(s => !s.IsDeleted && s.Iccid == iccid && s.Status == SimStatus.Available)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var profile = await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new { p.Id, p.CustomerId })
            .FirstAsync(cancellationToken);

        await using var kycStream = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 E2E-KYC-ALT"));
        var kycRef = await kyc.StoreKycDocumentAsync(
            msisdnAsset.Msisdn,
            "e2e-kyc-alt.pdf",
            kycStream,
            "application/pdf",
            cancellationToken);

        return new ActivationSeedBundle(
            profile.Id,
            profile.CustomerId,
            msisdnAsset.Id,
            msisdnAsset.Msisdn,
            simId,
            offering,
            kycRef);
    }

    public static async Task<string> ResolveSubscriptionIdForLineAsync(
        IServiceProvider services,
        string subscriberProfileId,
        string? msisdnAssetId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();

        var subsQuery = query.TelecomSubscription.AsNoTracking()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == subscriberProfileId);

        if (!string.IsNullOrEmpty(msisdnAssetId))
        {
            subsQuery = subsQuery.Where(s => s.MsisdnAssetId == msisdnAssetId);
        }

        var subscriptionId = await subsQuery
            .OrderByDescending(s => s.IsPrimaryLine)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return subscriptionId
            ?? throw new InvalidOperationException(
                $"No TelecomSubscription for profile {subscriberProfileId} / asset {msisdnAssetId}.");
    }

    public static async Task<TelecomOperationStatus> WaitForOperationStatusAsync(
        IServiceProvider services,
        string operationId,
        Func<TelecomOperationStatus, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var status = await db.TelecomOperationRequest.AsNoTracking()
                .Where(o => !o.IsDeleted && o.Id == operationId)
                .Select(o => o.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (predicate(status))
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }

        throw new TimeoutException($"Operation {operationId} did not reach expected status within {timeout}.");
    }

    public static async Task WaitForOutboxProcessedAsync(
        IServiceProvider services,
        string correlationId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var processed = await db.IntegrationOutboxMessage.AsNoTracking()
                .AnyAsync(
                    m => !m.IsDeleted
                         && m.CorrelationId == correlationId
                         && m.ProcessedAtUtc != null,
                    cancellationToken);

            if (processed)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        throw new TimeoutException($"Outbox message for correlation {correlationId} was not processed within {timeout}.");
    }

    public static async Task WaitForPendingOutboxAsync(
        IServiceProvider services,
        string correlationId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var pending = await db.IntegrationOutboxMessage.AsNoTracking()
                .AnyAsync(
                    m => !m.IsDeleted
                         && m.CorrelationId == correlationId
                         && m.ProcessedAtUtc == null,
                    cancellationToken);

            if (pending)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        }

        throw new TimeoutException($"Outbox message for correlation {correlationId} was not enqueued within {timeout}.");
    }

    public static async Task BackdateScheduledOperationEffectiveDateAsync(
        IServiceProvider services,
        string operationId,
        DateTime effectiveDateUtc,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var op = await db.TelecomOperationRequest
            .FirstAsync(o => !o.IsDeleted && o.Id == operationId, cancellationToken);

        if (op.Status != TelecomOperationStatus.Scheduled)
        {
            throw new InvalidOperationException(
                $"Operation {operationId} must be Scheduled to backdate (current: {op.Status}).");
        }

        switch (op.Kind)
        {
            case TelecomOperationKind.Migration:
                op.MigrationEffectiveDateUtc = effectiveDateUtc;
                break;
            case TelecomOperationKind.ChangeGsmType:
                op.GsmEffectiveDateUtc = effectiveDateUtc;
                break;
            case TelecomOperationKind.TakeOver:
                op.TakeOverEffectiveDateUtc = effectiveDateUtc;
                break;
            case TelecomOperationKind.Termination:
                op.TerminationEffectiveDateUtc = effectiveDateUtc;
                break;
            case TelecomOperationKind.TemporarySuspension:
                op.SuspensionStartDateUtc = effectiveDateUtc;
                break;
            case TelecomOperationKind.NumberPortability:
                op.NumberChangeEffectiveDateUtc = effectiveDateUtc;
                break;
            case TelecomOperationKind.SimSwap:
                op.SimSwapEffectiveDateUtc = effectiveDateUtc;
                break;
            default:
                throw new NotSupportedException($"Kind {op.Kind} does not support scheduled effective dates.");
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<TelecomActivationWorkflowResult> ExecuteDueScheduledOperationAsync(
        IServiceProvider services,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var workflow = scope.ServiceProvider.GetRequiredService<ITelecomActivationWorkflow>();
        return await workflow.ExecuteScheduledOperationAsync(
            operationId,
            SystemOperatorContext.SystemUserId,
            cancellationToken);
    }
}
