using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.SimSwap;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class SimSwapConfirmStrategy : IOperationConfirmStrategy
{
    private const string LostStolenPreSwapSuspensionReason =
        "Automated pre-swap lockdown for lost/stolen asset recovery";

    private readonly ISimSwapEligibilityChecker _eligibility;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;

    public SimSwapConfirmStrategy(
        ISimSwapEligibilityChecker eligibility,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository)
    {
        _eligibility = eligibility;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.SimSwap;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.SimInventoryId))
        {
            return new(false, "الشريحة الجديدة غير محددة في الطلب.");
        }

        entity.PriorSimInventoryId ??= await ResolvePriorSimIdForSwapAsync(entity, cancellationToken);

        var check = await _eligibility.ValidateForConfirmAsync(entity, cancellationToken);
        if (!check.Allowed)
        {
            return new(false, check.MessageAr);
        }

        if (!string.IsNullOrEmpty(check.PriorSimInventoryId))
        {
            entity.PriorSimInventoryId = check.PriorSimInventoryId;
        }

        return new(true, check.MessageAr);
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.SimInventoryId))
        {
            return null;
        }

        await EnsureLostStolenPreSwapSuspensionAsync(entity, actorUserId, cancellationToken);

        var newSim = await _simRepository.GetAsync(entity.SimInventoryId!, cancellationToken)
            ?? throw new BusinessRuleViolationException("الشريحة الجديدة غير موجودة.");

        if (newSim.Status is not (SimStatus.Available or SimStatus.Reserved))
        {
            throw new BusinessRuleViolationException("الشريحة الجديدة غير متاحة للتبديل.");
        }

        entity.PriorSimInventoryId ??= await ResolvePriorSimIdForSwapAsync(entity, cancellationToken);

        if (!string.IsNullOrEmpty(entity.PriorSimInventoryId))
        {
            var priorSim = await _simRepository.GetAsync(entity.PriorSimInventoryId, cancellationToken);
            if (priorSim != null && priorSim.Status == SimStatus.Active)
            {
                if (entity.IsLostOrStolenReport)
                {
                    priorSim.MarkBurned(DateTime.UtcNow);
                }
                else
                {
                    priorSim.TransitionTo(SimStatus.Quarantined);
                }

                priorSim.UpdatedById = actorUserId;
                _simRepository.Update(priorSim);
            }
        }

        var oldSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == entity.SubscriberProfileId
                && s.Status == SimStatus.Active
                && s.Id != entity.SimInventoryId
                && s.Id != entity.PriorSimInventoryId)
            .ToListAsync(cancellationToken);

        foreach (var old in oldSims)
        {
            old.TransitionTo(SimStatus.Quarantined);
            old.UpdatedById = actorUserId;
            _simRepository.Update(old);
        }

        if (newSim.Status == SimStatus.Available)
        {
            newSim.TransitionTo(SimStatus.Reserved);
        }

        newSim.TransitionTo(SimStatus.Active);
        newSim.AssignToProfile(entity.SubscriberProfileId);
        newSim.UpdatedById = actorUserId;
        _simRepository.Update(newSim);

        string? msisdn = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var msisdnAsset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
            if (msisdnAsset != null)
            {
                msisdn = msisdnAsset.Msisdn;
                msisdnAsset.PairedIccid = newSim.Iccid;
                msisdnAsset.PairedImsi = newSim.Imsi;
                if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Suspended)
                {
                    msisdnAsset.TransitionTo(MsisdnPoolStatus.Active);
                }

                msisdnAsset.UpdatedById = actorUserId;
                _msisdnRepository.Update(msisdnAsset);
            }
        }

        return new OperationApplyResult(msisdn, newSim.Iccid, null, entity.SubscriberProfileId, null);
    }

    private async Task EnsureLostStolenPreSwapSuspensionAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (!entity.IsLostOrStolenReport)
        {
            return;
        }

        var profile = await _profileRepository.GetAsync(entity.SubscriberProfileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        MsisdnAsset? msisdnAsset = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            msisdnAsset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
        }

        var alreadySuspended = profile.OperationalStatus is SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound
            || msisdnAsset?.PoolStatus == MsisdnPoolStatus.Suspended;

        if (alreadySuspended)
        {
            return;
        }

        entity.SuspensionType ??= SuspensionWellKnown.Operational;
        entity.SuspensionReason ??= LostStolenPreSwapSuspensionReason;
        entity.BarringLevel ??= SuspensionWellKnown.BarringFull;
        entity.PriorOperationalStatus ??= profile.OperationalStatus.ToString();
        entity.BarStatus ??= "Pending";
        entity.SuspensionStartDateUtc ??= DateTime.UtcNow;

        profile.Suspend(msisdnAsset?.Msisdn ?? "");
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        if (msisdnAsset != null && msisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            msisdnAsset.TransitionTo(MsisdnPoolStatus.Suspended);
            msisdnAsset.UpdatedById = actorUserId;
            _msisdnRepository.Update(msisdnAsset);
        }
    }

    private async Task<string?> ResolvePriorSimIdForSwapAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(entity.PriorSimInventoryId))
        {
            return entity.PriorSimInventoryId;
        }

        return await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == entity.SubscriberProfileId
                && s.Status == SimStatus.Active
                && s.Id != entity.SimInventoryId)
            .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
