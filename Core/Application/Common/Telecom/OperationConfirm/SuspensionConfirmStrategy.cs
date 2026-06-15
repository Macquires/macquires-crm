using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.Suspension;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class SuspensionConfirmStrategy : IOperationConfirmStrategy
{
    private readonly ISuspensionEligibilityChecker _eligibility;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;

    public SuspensionConfirmStrategy(
        ISuspensionEligibilityChecker eligibility,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository)
    {
        _eligibility = eligibility;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.TemporarySuspension;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return new(false, "رقم الخط غير محدد في طلب الحظر.");
        }

        var check = await _eligibility.ValidateForConfirmAsync(entity, cancellationToken);
        return new(check.Allowed, check.MessageAr);
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

        entity.PriorOperationalStatus ??= profile.OperationalStatus.ToString();
        var msisdn = (await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken))?.Msisdn ?? "";
        OperationConfirmBarring.ApplyToProfile(profile, entity.BarringLevel ?? SuspensionWellKnown.BarringFull, msisdn);
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        var msisdnAsset = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("أصل الرقم غير موجود.");

        if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            msisdnAsset.TransitionTo(MsisdnPoolStatus.Suspended);
        }

        msisdnAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(msisdnAsset);

        entity.BarStatus = "Pending";
        entity.SuspensionStartDateUtc ??= DateTime.UtcNow;

        return new OperationApplyResult(msisdn, null, null, profile.Id, null);
    }
}
