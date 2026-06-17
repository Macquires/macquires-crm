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

public sealed record ShowcaseLineRef(
    string CustomerId,
    string SubscriberProfileId,
    string MsisdnAssetId,
    string Msisdn,
    string LineKey);

public sealed record RechargeLedgerSnapshot(
    string PaymentId,
    string PaymentNumber,
    decimal Amount,
    string GatewayReference,
    string? ConfirmedByUserId,
    DateTime? ConfirmedAtUtc,
    PaymentTransactionStatus Status);

public sealed record SubscriberVasSnapshot(
    string Id,
    SubscriberVasStatus Status,
    DateTime? ActivatedAtUtc,
    DateTime? DeactivatedAtUtc);

public sealed record TelecomOperationSnapshot(
    string Id,
    string Number,
    TelecomOperationKind Kind,
    TelecomOperationStatus Status,
    string? SubscriberProfileId);

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
        return await ResolveDemoCustomerIdForMsisdnAsync(
            services,
            TelecomDemoMsisdn.ShowcaseHealthy,
            cancellationToken);
    }

    public static async Task<string> ResolveDemoCustomerIdForMsisdnAsync(
        IServiceProvider services,
        string msisdn,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var customerId = await (
            from asset in query.MsisdnAsset.AsNoTracking()
            join profile in query.SubscriberProfile.AsNoTracking() on asset.SubscriberProfileId equals profile.Id
            where !asset.IsDeleted
                  && !profile.IsDeleted
                  && asset.Msisdn == msisdn
                  && asset.SubscriberProfileId != null
            select profile.CustomerId
        ).FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidOperationException($"No customer found for demo MSISDN {msisdn}.");
        }

        return customerId;
    }

    public static async Task<ShowcaseLineRef> ResolveShowcaseLineAsync(
        IServiceProvider services,
        string msisdn,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        var row = await query.MsisdnAsset.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Msisdn == msisdn && m.SubscriberProfileId != null)
            .Select(m => new { m.Id, m.Msisdn, m.SubscriberProfileId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Demo line {msisdn} not found.");

        var profile = await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == row.SubscriberProfileId)
            .Select(p => new { p.Id, p.CustomerId })
            .FirstAsync(cancellationToken);

        var lineKey = $"{profile.Id}|{row.Msisdn}|{row.Id}";
        return new ShowcaseLineRef(profile.CustomerId, profile.Id, row.Id, row.Msisdn, lineKey);
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

    public static async Task<decimal?> GetSubscriberPrepaidBalanceAsync(
        IServiceProvider services,
        string subscriberProfileId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        return await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == subscriberProfileId)
            .Select(p => p.PrepaidBalance)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<string?> GetSubscriptionOfferingCodeAsync(
        IServiceProvider services,
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        return await (
            from s in query.TelecomSubscription.AsNoTracking()
            join o in query.ProductOffering.AsNoTracking() on s.ProductOfferingId equals o.Id
            where !s.IsDeleted && s.Id == subscriptionId
            select o.Code
        ).FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<string?> GetTechnicalTicketIdByNumberAsync(
        IServiceProvider services,
        string ticketNumber,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.TelecomTechnicalTicket.AsNoTracking()
            .Where(t => !t.IsDeleted && t.TicketNumber == ticketNumber)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<RechargeLedgerSnapshot?> GetLatestCompletedRechargeByGatewayRefAsync(
        IServiceProvider services,
        string customerId,
        string gatewayReference,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var row = await db.TelecomPaymentTransaction.AsNoTracking()
            .Where(p => !p.IsDeleted
                        && p.CustomerId == customerId
                        && p.GatewayReference == gatewayReference
                        && p.Status == PaymentTransactionStatus.Completed)
            .OrderByDescending(p => p.ConfirmedAtUtc)
            .Select(p => new RechargeLedgerSnapshot(
                p.Id,
                p.Number,
                p.Amount,
                p.GatewayReference ?? "",
                p.CreatedById,
                p.ConfirmedAtUtc,
                p.Status))
            .FirstOrDefaultAsync(cancellationToken);

        return row;
    }

    public static async Task<SubscriberVasSnapshot?> GetSubscriberVasRowAsync(
        IServiceProvider services,
        string subscriptionId,
        string serviceCode,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        return await (
            from row in query.SubscriberActiveService.AsNoTracking()
            join vas in query.TelecomValueAddedService.AsNoTracking() on row.TelecomValueAddedServiceId equals vas.Id
            where !row.IsDeleted
                  && row.TelecomSubscriptionId == subscriptionId
                  && vas.ServiceCode == serviceCode
            select new SubscriberVasSnapshot(
                row.Id,
                row.Status,
                row.ActivatedAtUtc,
                row.DeactivatedAtUtc)
        ).FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task TransitionTechnicalTicketStatusAsync(
        IServiceProvider services,
        string ticketId,
        TechnicalTicketStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var ticket = await db.TelecomTechnicalTicket
            .FirstAsync(t => !t.IsDeleted && t.Id == ticketId, cancellationToken);

        ticket.Status = newStatus;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        if (newStatus == TechnicalTicketStatus.InProgress)
        {
            ticket.ResolvedAtUtc = null;
            ticket.ResolvedByUserId = null;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<bool> HasAuditLogForEntityAsync(
        IServiceProvider services,
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.UserAuditLog.AsNoTracking()
            .AnyAsync(
                l => !l.IsDeleted
                     && l.EntityType == entityType
                     && l.EntityId == entityId,
                cancellationToken);
    }

    public static async Task<int> CountActiveSubscriberVasRowsAsync(
        IServiceProvider services,
        string subscriptionId,
        string serviceCode,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        return await (
            from row in query.SubscriberActiveService.AsNoTracking()
            join vas in query.TelecomValueAddedService.AsNoTracking() on row.TelecomValueAddedServiceId equals vas.Id
            where !row.IsDeleted
                  && row.TelecomSubscriptionId == subscriptionId
                  && vas.ServiceCode == serviceCode
                  && row.Status == SubscriberVasStatus.Active
            select row.Id
        ).CountAsync(cancellationToken);
    }

    public static async Task<TelecomOperationSnapshot?> GetLatestTelecomOperationByKindAsync(
        IServiceProvider services,
        string subscriberProfileId,
        TelecomOperationKind kind,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.SubscriberProfileId == subscriberProfileId
                        && o.Kind == kind)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new TelecomOperationSnapshot(
                o.Id,
                o.Number,
                o.Kind,
                o.Status,
                o.SubscriberProfileId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<SubscriberOperationalStatus?> GetSubscriberOperationalStatusAsync(
        IServiceProvider services,
        string subscriberProfileId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
        return await query.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == subscriberProfileId)
            .Select(p => p.OperationalStatus)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
