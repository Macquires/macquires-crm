using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Common.Telecom.ChangeNumber;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class ChangeNumberConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IChangeNumberEligibilityChecker _eligibility;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly IOperationConfirmProvisionContextBuilder _provisionContextBuilder;

    public ChangeNumberConfirmStrategy(
        IChangeNumberEligibilityChecker eligibility,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        IOperationConfirmProvisionContextBuilder provisionContextBuilder)
    {
        _eligibility = eligibility;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _provisionContextBuilder = provisionContextBuilder;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.NumberPortability;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (!ChangeNumberWellKnown.IsPortInMode(entity.NumberChangeMode)
            && string.IsNullOrEmpty(entity.TargetMsisdnAssetId))
        {
            return new(false, "الرقم المستهدف غير محدد في الطلب.");
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
        if (ChangeNumberWellKnown.IsPortInMode(entity.NumberChangeMode))
        {
            return await ApplyPortInLocalChangesAsync(entity, actorUserId, cancellationToken);
        }

        if (string.IsNullOrEmpty(entity.TargetMsisdnAssetId))
        {
            return null;
        }

        var priorId = entity.PriorMsisdnAssetId ?? entity.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("الرقم الحالي غير محدد.");
        var targetId = entity.TargetMsisdnAssetId
            ?? throw new BusinessRuleViolationException("الرقم المستهدف غير محدد.");

        var priorAsset = await _msisdnRepository.GetAsync(priorId, cancellationToken)
            ?? throw new BusinessRuleViolationException("أصل الرقم الحالي غير موجود.");
        var targetAsset = await _msisdnRepository.GetAsync(targetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("أصل الرقم المستهدف غير موجود.");

        if (targetAsset.PoolStatus == MsisdnPoolStatus.Quarantined)
        {
            throw new BusinessRuleViolationException("VAL-05-03: الرقم المستهدف في حجر صحي.");
        }

        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == entity.SubscriberProfileId
                        && s.MsisdnAssetId == priorId)
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            sub.MsisdnAssetId = targetId;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
        }

        if (priorAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            priorAsset.TransitionTo(MsisdnPoolStatus.Quarantined);
        }

        priorAsset.SubscriberProfileId = null;
        priorAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(priorAsset);

        if (targetAsset.PoolStatus == MsisdnPoolStatus.Reserved)
        {
            targetAsset.TransitionTo(MsisdnPoolStatus.Active);
        }
        else if (targetAsset.PoolStatus == MsisdnPoolStatus.Available)
        {
            targetAsset.TransitionTo(MsisdnPoolStatus.Active);
        }

        targetAsset.SubscriberProfileId = entity.SubscriberProfileId;
        targetAsset.ReservedForCustomerId = null;
        targetAsset.ReservedUntilUtc = null;
        targetAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(targetAsset);

        var ctx = await _provisionContextBuilder.BuildForMsisdnChangeAsync(
            entity,
            targetAsset.Msisdn,
            priorAsset.Msisdn,
            cancellationToken);

        return new OperationApplyResult(
            ctx.Msisdn,
            ctx.Iccid,
            ctx.CustomerId,
            ctx.SubscriberProfileId,
            ctx.TelecomSubscriptionId);
    }

    private async Task<OperationApplyResult?> ApplyPortInLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var portMsisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(entity.PortInMsisdn ?? string.Empty)
            ?? throw new BusinessRuleViolationException("رقم النقل غير صالح.");

        var priorId = entity.PriorMsisdnAssetId ?? entity.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("الرقم الحالي غير محدد.");

        var priorAsset = await _msisdnRepository.GetAsync(priorId, cancellationToken)
            ?? throw new BusinessRuleViolationException("أصل الرقم الحالي غير موجود.");

        var targetAsset = await _msisdnRepository.GetQuery()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == portMsisdn, cancellationToken);

        if (targetAsset == null)
        {
            targetAsset = new MsisdnAsset
            {
                Msisdn = portMsisdn,
                PoolStatus = MsisdnPoolStatus.Available,
                Category = MsisdnCategory.Normal,
                CreatedById = actorUserId,
            };
            await _msisdnRepository.CreateAsync(targetAsset, cancellationToken);
        }
        else if (targetAsset.PoolStatus == MsisdnPoolStatus.Active
                 && !string.IsNullOrEmpty(targetAsset.SubscriberProfileId)
                 && !string.Equals(targetAsset.SubscriberProfileId, entity.SubscriberProfileId, StringComparison.Ordinal))
        {
            throw new BusinessRuleViolationException("VAL-MNP-04: رقم النقل مربوط بمشترك آخر.");
        }

        entity.TargetMsisdnAssetId = targetAsset.Id;

        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == entity.SubscriberProfileId
                        && s.MsisdnAssetId == priorId)
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            sub.MsisdnAssetId = targetAsset.Id;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
        }

        if (priorAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            priorAsset.TransitionTo(MsisdnPoolStatus.Quarantined);
        }

        priorAsset.SubscriberProfileId = null;
        priorAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(priorAsset);

        if (targetAsset.PoolStatus is MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved)
        {
            targetAsset.TransitionTo(MsisdnPoolStatus.Active);
        }

        targetAsset.SubscriberProfileId = entity.SubscriberProfileId;
        targetAsset.ReservedForCustomerId = null;
        targetAsset.ReservedUntilUtc = null;
        targetAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(targetAsset);

        entity.ProvisioningResult = "PortInCompleted";

        var ctx = await _provisionContextBuilder.BuildForMsisdnChangeAsync(
            entity,
            targetAsset.Msisdn,
            priorAsset.Msisdn,
            cancellationToken);

        return new OperationApplyResult(
            ctx.Msisdn,
            ctx.Iccid,
            ctx.CustomerId,
            ctx.SubscriberProfileId,
            ctx.TelecomSubscriptionId);
    }
}
