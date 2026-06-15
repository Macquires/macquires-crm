using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.Reconnect;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class ReconnectConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IReconnectEligibilityChecker _eligibility;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;

    public ReconnectConfirmStrategy(
        IReconnectEligibilityChecker eligibility,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository)
    {
        _eligibility = eligibility;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.Reconnect;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        OperationConfirmValidationHelpers.ApplyFraudClearanceIfRequired(entity, actorUserId);

        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return new(false, "رقم الخط غير محدد في طلب إعادة التفعيل.");
        }

        var check = await _eligibility.ValidateForConfirmAsync(entity, cancellationToken);
        if (!check.Allowed)
        {
            return new(false, check.MessageAr);
        }

        entity.SourceSuspensionOperationId ??= check.SourceSuspensionOperationId;
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

        entity.PriorOperationalStatus ??= profile.OperationalStatus.ToString();
        profile.Activate();
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        var msisdnAsset = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("أصل الرقم غير موجود.");

        if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Suspended)
        {
            msisdnAsset.TransitionTo(MsisdnPoolStatus.Active);
        }

        msisdnAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(msisdnAsset);

        entity.ProvisioningResult = "Pending";
        entity.ReactivationAtUtc ??= DateTime.UtcNow;

        return new OperationApplyResult(msisdnAsset.Msisdn, null, null, profile.Id, null);
    }
}
