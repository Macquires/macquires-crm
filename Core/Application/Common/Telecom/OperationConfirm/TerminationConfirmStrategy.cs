using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.Termination;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class TerminationConfirmStrategy : IOperationConfirmStrategy
{
    private readonly ITerminationEligibilityChecker _eligibility;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;

    public TerminationConfirmStrategy(
        ITerminationEligibilityChecker eligibility,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository)
    {
        _eligibility = eligibility;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.Termination;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return new(false, "رقم الخط غير محدد في طلب الإنهاء.");
        }

        entity.PriorMsisdnAssetId ??= entity.MsisdnAssetId;
        var check = await _eligibility.ValidateForConfirmAsync(entity, cancellationToken);
        if (!check.Allowed)
        {
            return new(false, check.MessageAr);
        }

        if (!string.IsNullOrEmpty(check.PriorSimInventoryId))
        {
            entity.PriorSimInventoryId ??= check.PriorSimInventoryId;
        }

        return new(true, check.MessageAr);
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var msisdnAssetId = entity.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("رقم الخط غير محدد.");

        var profile = await _profileRepository.GetAsync(entity.SubscriberProfileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        if (profile.OperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            throw new BusinessRuleViolationException("VAL-10-04: الخط منتهٍ مسبقاً.");
        }

        profile.Terminate();
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        var msisdnAsset = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("أصل الرقم غير موجود.");

        if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            msisdnAsset.TransitionTo(MsisdnPoolStatus.Quarantined);
        }

        msisdnAsset.SubscriberProfileId = null;
        msisdnAsset.ReservedForCustomerId = null;
        msisdnAsset.ReservedUntilUtc = null;
        msisdnAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(msisdnAsset);

        var activeSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == entity.SubscriberProfileId
                        && s.Status == SimStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var sim in activeSims)
        {
            sim.TransitionTo(SimStatus.Quarantined);
            sim.AssignToProfile(null);
            sim.UpdatedById = actorUserId;
            _simRepository.Update(sim);
        }

        entity.PriorSimInventoryId ??= activeSims
            .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
            .Select(s => s.Id)
            .FirstOrDefault();

        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == entity.SubscriberProfileId
                        && s.MsisdnAssetId == msisdnAssetId)
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            sub.IsDeleted = true;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
        }

        entity.DeprovisionStatus = "Pending";
        entity.PriorMsisdnAssetId ??= msisdnAssetId;

        return new OperationApplyResult(
            msisdnAsset.Msisdn,
            null,
            profile.CustomerId,
            profile.Id,
            null);
    }
}
