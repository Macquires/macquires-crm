using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Suspension;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Idempotent reconciliation for national demo anchors — keeps CBS, CRM, commercial catalog, and queues coherent.
/// </summary>
public sealed class DemoAnchorBaselineReconciler
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly ICommandRepository<TelecomPaymentTransaction> _paymentRepository;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBillingSystemIntegration? _billing;
    private readonly IHLRLiveStatusService? _hlr;

    public DemoAnchorBaselineReconciler(
        IQueryContext query,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<BillingIntegrationLog> logRepository,
        ICommandRepository<TelecomPaymentTransaction> paymentRepository,
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        IUnitOfWork unitOfWork,
        IBillingSystemIntegration? billing = null,
        IHLRLiveStatusService? hlr = null)
    {
        _query = query;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _operationRepository = operationRepository;
        _logRepository = logRepository;
        _paymentRepository = paymentRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _billing = billing;
        _hlr = hlr;
    }

    public async Task ReconcileAllAsync(CancellationToken cancellationToken = default)
    {
        await ReconcileDebtSubscriberAsync(cancellationToken);
    }

    /// <summary>
    /// مازن المديون 0939000002 — postpaid invoice debt, CRM suspended, CBS −15k, clean BO queue, revenue-leakage ready HLR.
    /// </summary>
    public async Task ReconcileDebtSubscriberAsync(CancellationToken cancellationToken = default)
    {
        var line = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == TelecomDemoMsisdn.DebtSubscriber)
            .Select(m => new { m.Id, m.SubscriberProfileId })
            .FirstOrDefaultAsync(cancellationToken);

        if (line == null || string.IsNullOrEmpty(line.SubscriberProfileId))
        {
            return;
        }

        var profileId = line.SubscriberProfileId;
        var assetId = line.Id;

        if (await IsDebtShowcaseSettledAsync(profileId, assetId, cancellationToken))
        {
            return;
        }

        await ReconcileDebtCommercialCatalogAsync(profileId, assetId, cancellationToken);
        await ReconcileDebtCrmFinancialStateAsync(profileId, assetId, cancellationToken);
        await ClearDebtBackOfficeQueueAsync(assetId, cancellationToken);
        await ClearDebtPreDemoPaymentArtifactsAsync(profileId, cancellationToken);
        await ResetDebtRevenueAssuranceTicketsAsync(profileId, cancellationToken);
        await EnsureDebtRevenueLeakageTicketAsync(profileId, cancellationToken);
        await ReconcileDebtExternalSystemsAsync(cancellationToken);
    }

    /// <summary>Reset CBS −15k; HLR stays ACTIVE (revenue leakage) while CRM is billing-suspended.</summary>
    private async Task ReconcileDebtExternalSystemsAsync(CancellationToken cancellationToken)
    {
        if (_billing != null)
        {
            try
            {
                await _billing.AdjustBalanceAsync(
                    TelecomDemoMsisdn.DebtSubscriber,
                    TelecomDemoBaselines.DebtOutstandingSyp,
                    "demo-anchor-reconcile",
                    cancellationToken: cancellationToken);
            }
            catch
            {
                // Simulator may be offline — in-process CBS mock is deterministic.
            }
        }

        if (_hlr != null)
        {
            try
            {
                await _hlr.MarkMockSubscriberActiveAsync(TelecomDemoMsisdn.DebtSubscriber, cancellationToken);
            }
            catch
            {
                // Best-effort HLR sandbox alignment for leakage demo.
            }
        }
    }

    /// <summary>Open RA ticket when CRM suspended + HLR active — visible immediately without waiting for the 12h job.</summary>
    private async Task EnsureDebtRevenueLeakageTicketAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetAsync(profileId, cancellationToken);
        if (profile == null
            || profile.OperationalStatus == SubscriberOperationalStatus.Active)
        {
            return;
        }

        var hasOpen = await _query.TelecomTechnicalTicket.AsNoTracking()
            .AnyAsync(
                t => !t.IsDeleted
                     && t.Msisdn == TelecomDemoMsisdn.DebtSubscriber
                     && t.TicketCategory == TechnicalTicketCategory.RevenueAssurance
                     && (t.Status == TechnicalTicketStatus.Open
                         || t.Status == TechnicalTicketStatus.InProgress),
                cancellationToken);

        if (hasOpen)
        {
            return;
        }

        var ticket = new TelecomTechnicalTicket
        {
            TicketNumber = $"RA-DEMO-{DateTime.UtcNow:yyyyMMddHHmmssfff}-0002",
            Msisdn = TelecomDemoMsisdn.DebtSubscriber,
            CustomerId = profile.CustomerId,
            SubscriberProfileId = profileId,
            TicketCategory = TechnicalTicketCategory.RevenueAssurance,
            IssueType = TechnicalTicketIssueType.Network,
            Priority = TechnicalTicketPriority.High,
            Status = TechnicalTicketStatus.Open,
            Notes =
                "Revenue Leakage Alert: CRM status is Suspended but HLR status is ACTIVE. Technical sync required to prevent unauthorized usage.|seed=debt-showcase",
            CreatedById = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
            OpenedByUserId = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
            CreatedByChannel = TechnicalTicketCreatedByChannel.SystemJob,
        };

        await _ticketRepository.CreateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    /// <summary>Skip baseline wipe when showroom PAY→RCN completed and CRM is Active with cleared ledger.</summary>
    private async Task<bool> IsDebtShowcaseSettledAsync(
        string profileId,
        string assetId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetAsync(profileId, cancellationToken);
        if (profile == null
            || profile.OperationalStatus != SubscriberOperationalStatus.Active
            || profile.PostpaidCreditLimit is < 0)
        {
            return false;
        }

        return await _operationRepository.GetQuery().AsNoTracking()
            .AnyAsync(
                o => !o.IsDeleted
                     && o.SubscriberProfileId == profileId
                     && o.MsisdnAssetId == assetId
                     && o.Kind == TelecomOperationKind.Reconnect
                     && o.Status == TelecomOperationStatus.Completed
                     && string.Equals(o.ClearanceType, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase),
                cancellationToken);
    }

    private async Task ReconcileDebtCommercialCatalogAsync(
        string profileId,
        string assetId,
        CancellationToken cancellationToken)
    {
        var offering = await _query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == TelecomDemoBaselines.DebtPostpaidOfferingCode)
            .Select(o => new { o.Id, o.ProductId })
            .FirstOrDefaultAsync(cancellationToken);

        if (offering?.ProductId == null)
        {
            return;
        }

        var asset = await _msisdnRepository.GetAsync(assetId, cancellationToken);
        if (asset != null)
        {
            asset.ProductId = offering.ProductId;
            asset.IntendedSubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Postpaid;
            _msisdnRepository.Update(asset);
        }

        var subscriptions = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == profileId && s.MsisdnAssetId == assetId)
            .ToListAsync(cancellationToken);

        foreach (var sub in subscriptions)
        {
            sub.ProductId = offering.ProductId;
            sub.ProductOfferingId = offering.Id;
            sub.SubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Postpaid;
            sub.IsPrimaryLine = true;
            _subscriptionRepository.Update(sub);
        }

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task ReconcileDebtCrmFinancialStateAsync(
        string profileId,
        string assetId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetAsync(profileId, cancellationToken);
        if (profile != null)
        {
            profile.PrepaidBalance = null;
            profile.PostpaidCreditLimit = TelecomDemoBaselines.DebtOutstandingSyp;
            profile.ActivationDateUtc = TelecomDemoBaselines.DebtLineActivatedUtc();
            profile.ChurnRiskScore ??= 68;
            profile.LoyaltyTier ??= "Silver";
            profile.LoyaltyPoints = profile.LoyaltyPoints <= 0 ? 1800 : profile.LoyaltyPoints;

            if (profile.OperationalStatus == SubscriberOperationalStatus.Active)
            {
                profile.Suspend(TelecomDemoMsisdn.DebtSubscriber);
            }

            _profileRepository.Update(profile);
        }

        var asset = await _msisdnRepository.GetAsync(assetId, cancellationToken);
        if (asset != null)
        {
            if (asset.PoolStatus == MsisdnPoolStatus.Active)
            {
                asset.TransitionTo(MsisdnPoolStatus.Suspended);
            }

            _msisdnRepository.Update(asset);
        }

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task ClearDebtBackOfficeQueueAsync(string assetId, CancellationToken cancellationToken)
    {
        var seedOps = _operationRepository.GetQuery().IgnoreQueryFilters();
        var pending = await seedOps
            .Where(o => !o.IsDeleted
                        && o.MsisdnAssetId == assetId
                        && (o.Kind == TelecomOperationKind.Reconnect
                            || o.Kind == TelecomOperationKind.BadDebtRecovery)
                        && (o.Status == TelecomOperationStatus.PendingDocuments
                            || o.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
                            || o.Status == TelecomOperationStatus.In_Progress
                            || o.Status == TelecomOperationStatus.Approved_Pending_Cash))
            .ToListAsync(cancellationToken);

        foreach (var op in pending)
        {
            op.Status = TelecomOperationStatus.Failed;
            op.CollectionSettlementStatus = BadDebtWellKnown.SettlementFailed;
            op.Notes = AppendSeedNote(
                op.Notes,
                "seed=clean-slate|reason=debt-demo-starts-from-showroom-pay");
            _operationRepository.Update(op);
        }

        if (pending.Count > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }
    }

    private async Task ClearDebtPreDemoPaymentArtifactsAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        const string receipt = TelecomDemoBaselines.DebtFullPaymentReceipt;

        var logs = await _logRepository.GetQuery().IgnoreQueryFilters()
            .Where(l => !l.IsDeleted && l.CorrelationId == receipt)
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            log.IsDeleted = true;
            _logRepository.Update(log);
        }

        var payments = await _paymentRepository.GetQuery().IgnoreQueryFilters()
            .Where(p => !p.IsDeleted
                        && p.SubscriberProfileId == profileId
                        && (p.Msisdn == TelecomDemoMsisdn.DebtSubscriber
                            || p.GatewayReference == receipt
                            || p.ReceiptNumber == receipt
                            || p.Number == receipt
                            || p.GatewayTransactionId == receipt))
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
        {
            payment.IsDeleted = true;
            _paymentRepository.Update(payment);
        }

        if (logs.Count > 0 || payments.Count > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }
    }

    /// <summary>Remove stale resolved RA tickets so the background job can raise a fresh Open leakage alert.</summary>
    private async Task ResetDebtRevenueAssuranceTicketsAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        var stale = await _ticketRepository.GetQuery().IgnoreQueryFilters()
            .Where(t => !t.IsDeleted
                        && t.Msisdn == TelecomDemoMsisdn.DebtSubscriber
                        && t.TicketCategory == TechnicalTicketCategory.RevenueAssurance
                        && t.Status == TechnicalTicketStatus.Resolved)
            .ToListAsync(cancellationToken);

        foreach (var ticket in stale)
        {
            ticket.IsDeleted = true;
            ticket.Notes = AppendSeedNote(ticket.Notes, "seed=reset|reason=revenue-leakage-demo-reopen");
            _ticketRepository.Update(ticket);
        }

        if (stale.Count > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }
    }

    private static string AppendSeedNote(string? existing, string suffix)
    {
        var baseNote = (existing ?? string.Empty).Trim();
        return string.IsNullOrEmpty(baseNote) ? suffix : $"{baseNote}|{suffix}";
    }
}
