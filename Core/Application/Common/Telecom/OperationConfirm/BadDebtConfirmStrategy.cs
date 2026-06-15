using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.BadDebt;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class BadDebtConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IBadDebtEligibilityChecker _eligibility;
    private readonly IBadDebtCompletionService _completion;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;

    public BadDebtConfirmStrategy(
        IBadDebtEligibilityChecker eligibility,
        IBadDebtCompletionService completion,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository)
    {
        _eligibility = eligibility;
        _completion = completion;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.BadDebtRecovery;

    public bool RequiresNetworkProvision => false;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return new(false, "رقم الخط غير محدد في طلب التحصيل.");
        }

        OperationConfirmValidationHelpers.ApplyFraudClearanceIfRequired(entity, actorUserId);
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

        entity.PriorDunningStage = entity.DunningStage;

        if (string.Equals(entity.DunningStage, BadDebtWellKnown.HardBar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity.CollectionAction, BadDebtWellKnown.DunningEscalation, StringComparison.OrdinalIgnoreCase)
                && string.Equals(entity.DunningStage, BadDebtWellKnown.HardBar, StringComparison.OrdinalIgnoreCase))
        {
            var profile = await _profileRepository.GetAsync(entity.SubscriberProfileId, cancellationToken)
                ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

            entity.PriorOperationalStatus ??= profile.OperationalStatus.ToString();
            if (profile.OperationalStatus == SubscriberOperationalStatus.Active)
            {
                var msisdnForSuspend = (await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken))?.Msisdn ?? "";
                profile.Suspend(msisdnForSuspend);
                profile.UpdatedById = actorUserId;
                _profileRepository.Update(profile);
            }

            var msisdnAsset = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken)
                ?? throw new BusinessRuleViolationException("أصل الرقم غير موجود.");

            if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
            {
                msisdnAsset.TransitionTo(MsisdnPoolStatus.Suspended);
                msisdnAsset.UpdatedById = actorUserId;
                _msisdnRepository.Update(msisdnAsset);
            }

            entity.BarStatus = "BillingBar";
        }

        if (string.Equals(entity.CollectionAction, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity.CollectionAction, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase))
        {
            entity.CollectionSettlementStatus = BadDebtWellKnown.SettlementCompleted;
            entity.DunningStage = BadDebtWellKnown.Settled;
        }

        entity.ProvisioningResult = "Pending";
        await _completion.FulfillAsync(entity, actorUserId, cancellationToken);

        return new OperationApplyResult(null, null, null, entity.SubscriberProfileId, null, RequiresBilling: true);
    }
}
