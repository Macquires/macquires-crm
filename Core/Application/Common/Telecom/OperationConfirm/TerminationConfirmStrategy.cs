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
        return new(check.Allowed, check.MessageAr);
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var msisdnAssetId = entity.MsisdnAssetId!;
        var profile = await _profileRepository.GetAsync(entity.SubscriberProfileId, cancellationToken)
            ?? throw new InvalidOperationException("ملف المشترك غير موجود.");

        profile.Terminate();
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        var msisdnAsset = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken)
            ?? throw new InvalidOperationException("أصل الرقم غير موجود.");

        if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            msisdnAsset.TransitionTo(MsisdnPoolStatus.Quarantined);
        }

        msisdnAsset.SubscriberProfileId = null;
        msisdnAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(msisdnAsset);

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
