using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Common.Telecom.Inventory;

public interface IMsisdnRecyclingService
{
    Task<int> ReleaseExpiredQuarantineAsync(CancellationToken cancellationToken = default);

    Task<bool> ForceRecycleLineAsync(
        string msisdnAssetId,
        string? actorUserId,
        CancellationToken cancellationToken = default);

    Task<int> ScanAndRecycleDormantLinesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Case C — MSISDN ghost unpairing: burn SIM, quarantine MSISDN, later release to Available.</summary>
public sealed class MsisdnRecyclingService : IMsisdnRecyclingService
{
    private const string DormantScannerActor = "dormant-line-scanner";

    private readonly IQueryContext _query;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ITelecomInventoryRulesProvider _rules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MsisdnRecyclingService> _logger;

    public MsisdnRecyclingService(
        IQueryContext query,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ITelecomInventoryRulesProvider rules,
        IUnitOfWork unitOfWork,
        ILogger<MsisdnRecyclingService> logger)
    {
        _query = query;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _profileRepository = profileRepository;
        _subscriptionRepository = subscriptionRepository;
        _rules = rules;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ReleaseExpiredQuarantineAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var released = 0;

        var msisdns = await _msisdnRepository.GetQuery()
            .Where(m => !m.IsDeleted
                && m.PoolStatus == MsisdnPoolStatus.Quarantined
                && m.QuarantineEndsUtc != null
                && m.QuarantineEndsUtc <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var asset in msisdns)
        {
            asset.ReleaseQuarantineIfExpired(utcNow);
            asset.SubscriberProfileId = null;
            asset.PairedIccid = null;
            asset.PairedImsi = null;
            asset.UpdatedAtUtc = utcNow;
            _msisdnRepository.Update(asset);
            released++;
        }

        var sims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.Status == SimStatus.Quarantined
                && s.QuarantineEndsUtc != null
                && s.QuarantineEndsUtc <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var sim in sims)
        {
            sim.ReleaseQuarantineIfExpired(utcNow);
            sim.AssignToProfile(null);
            sim.UpdatedAtUtc = utcNow;
            _simRepository.Update(sim);
            released++;
        }

        if (released > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return released;
    }

    public async Task<bool> ForceRecycleLineAsync(
        string msisdnAssetId,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var asset = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken);
        if (asset == null || asset.IsDeleted)
        {
            return false;
        }

        if (asset.PoolStatus is not (MsisdnPoolStatus.Active or MsisdnPoolStatus.Suspended))
        {
            return false;
        }

        var quarantineDays = await _rules.GetMsisdnQuarantineDaysAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;
        var profileId = asset.SubscriberProfileId;

        if (!string.IsNullOrEmpty(profileId))
        {
            var profile = await _profileRepository.GetAsync(profileId, cancellationToken);
            if (profile != null && profile.OperationalStatus != SubscriberOperationalStatus.Terminated)
            {
                profile.Terminate();
                profile.UpdatedById = actorUserId;
                _profileRepository.Update(profile);
            }

            var subs = await _subscriptionRepository.GetQuery()
                .Where(s => !s.IsDeleted
                    && s.SubscriberProfileId == profileId
                    && s.MsisdnAssetId == asset.Id)
                .ToListAsync(cancellationToken);

            foreach (var sub in subs)
            {
                sub.IsDeleted = true;
                sub.UpdatedById = actorUserId;
                _subscriptionRepository.Update(sub);
            }
        }

        var activeSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == profileId
                && (s.Status == SimStatus.Active || s.Status == SimStatus.Suspended))
            .ToListAsync(cancellationToken);

        foreach (var sim in activeSims)
        {
            sim.MarkBurned(utcNow);
            sim.AssignToProfile(null);
            sim.UpdatedById = actorUserId;
            _simRepository.Update(sim);
        }

        asset.SubscriberProfileId = null;
        asset.PairedIccid = null;
        asset.PairedImsi = null;
        asset.TransitionTo(MsisdnPoolStatus.Quarantined, quarantineDays);
        asset.UpdatedById = actorUserId;
        _msisdnRepository.Update(asset);

        await _unitOfWork.SaveAsync(cancellationToken);
        return true;
    }

    public async Task<int> ScanAndRecycleDormantLinesAsync(CancellationToken cancellationToken = default)
    {
        var inactivityDays = await _rules.GetDormantLineInactivityDaysAsync(cancellationToken);
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(30, inactivityDays));
        var recycled = 0;

        var protectedProfileIds = await LoadObligationProtectedProfileIdsAsync(cancellationToken);
        var candidates = await LoadDormantPrepaidCandidatesAsync(cutoff, protectedProfileIds, cancellationToken);

        foreach (var candidate in candidates)
        {
            try
            {
                var ok = await ForceRecycleLineAsync(candidate.MsisdnAssetId, DormantScannerActor, cancellationToken);
                if (ok)
                {
                    recycled++;
                    _logger.LogInformation(
                        "Dormant prepaid line recycled: MSISDN {Msisdn}, asset {AssetId}, last activity {LastActivity:u}",
                        candidate.Msisdn,
                        candidate.MsisdnAssetId,
                        candidate.LastActivityUtc);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to recycle dormant line asset {AssetId} ({Msisdn})",
                    candidate.MsisdnAssetId,
                    candidate.Msisdn);
            }
        }

        return recycled;
    }

    private async Task<HashSet<string>> LoadObligationProtectedProfileIdsAsync(CancellationToken cancellationToken)
    {
        var activeContractStatuses = new[]
        {
            InstallmentContractStatus.Active,
            InstallmentContractStatus.Delinquent,
        };

        var profileIds = await (
            from op in _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            join contract in _query.DeviceInstallmentContract.AsNoTracking().IsDeletedEqualTo()
                on op.Id equals contract.TelecomOperationRequestId
            where op.DeviceInstallmentContractId != null
                  && activeContractStatuses.Contains(contract.Status)
            select op.SubscriberProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return profileIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<List<DormantLineCandidate>> LoadDormantPrepaidCandidatesAsync(
        DateTime activityCutoffUtc,
        HashSet<string> protectedProfileIds,
        CancellationToken cancellationToken)
    {
        var operationalStatuses = new[]
        {
            SubscriberOperationalStatus.Active,
            SubscriberOperationalStatus.Suspended,
            SubscriberOperationalStatus.SuspendedInbound,
            SubscriberOperationalStatus.SuspendedOutbound,
        };

        var poolStatuses = new[] { MsisdnPoolStatus.Active, MsisdnPoolStatus.Suspended };

        var rows = await (
            from asset in _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            join sub in _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                on asset.Id equals sub.MsisdnAssetId
            join profile in _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                on asset.SubscriberProfileId equals profile.Id
            where asset.SubscriberProfileId != null
                  && poolStatuses.Contains(asset.PoolStatus)
                  && sub.SubscriptionTypeId == TelecomSubscriptionTypeWellKnownIds.Prepaid
                  && operationalStatuses.Contains(profile.OperationalStatus)
            select new
            {
                asset.Id,
                asset.Msisdn,
                ProfileId = profile.Id,
                profile.ActivationDateUtc,
                asset.UpdatedAtUtc,
                asset.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var profileIds = rows.Select(r => r.ProfileId).Distinct().ToList();
        var lastRechargeByProfile = await _query.TelecomPaymentTransaction.AsNoTracking().IsDeletedEqualTo()
            .Where(p => profileIds.Contains(p.SubscriberProfileId)
                && p.Status == PaymentTransactionStatus.Completed
                && (p.TransactionType == PaymentTransactionType.Recharge
                    || p.TransactionType == PaymentTransactionType.VoucherRedeem
                    || p.TransactionType == PaymentTransactionType.WalletTopUp))
            .GroupBy(p => p.SubscriberProfileId)
            .Select(g => new
            {
                ProfileId = g.Key,
                LastRechargeUtc = g.Max(p => p.ConfirmedAtUtc ?? p.CreatedAtUtc),
            })
            .ToDictionaryAsync(x => x.ProfileId, x => x.LastRechargeUtc, cancellationToken);

        var dormant = new List<DormantLineCandidate>();

        foreach (var row in rows)
        {
            if (protectedProfileIds.Contains(row.ProfileId))
            {
                continue;
            }

            lastRechargeByProfile.TryGetValue(row.ProfileId, out var lastRechargeUtc);
            var lastActivity = ResolveLastActivityUtc(
                lastRechargeUtc,
                row.ActivationDateUtc,
                row.UpdatedAtUtc,
                row.CreatedAtUtc);

            if (!lastActivity.HasValue || lastActivity.Value > activityCutoffUtc)
            {
                continue;
            }

            dormant.Add(new DormantLineCandidate(row.Id, row.Msisdn, lastActivity.Value));
        }

        return dormant;
    }

    private static DateTime? ResolveLastActivityUtc(
        DateTime? lastRechargeUtc,
        DateTime? activationDateUtc,
        DateTime? assetUpdatedUtc,
        DateTime? assetCreatedUtc)
    {
        DateTime? max = null;
        foreach (var candidate in new[] { lastRechargeUtc, activationDateUtc, assetUpdatedUtc, assetCreatedUtc })
        {
            if (!candidate.HasValue)
            {
                continue;
            }

            max = max.HasValue
                ? (candidate.Value > max.Value ? candidate.Value : max.Value)
                : candidate.Value;
        }

        return max;
    }

    private sealed record DormantLineCandidate(string MsisdnAssetId, string Msisdn, DateTime LastActivityUtc);
}
