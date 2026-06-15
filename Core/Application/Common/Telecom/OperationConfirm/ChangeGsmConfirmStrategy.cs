using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.ChangeGsm;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class ChangeGsmConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IChangeGsmEligibilityChecker _eligibility;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;

    public ChangeGsmConfirmStrategy(
        IChangeGsmEligibilityChecker eligibility,
        ICommandRepository<TelecomSubscription> subscriptionRepository)
    {
        _eligibility = eligibility;
        _subscriptionRepository = subscriptionRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.ChangeGsmType;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.TargetSubscriptionTypeId))
        {
            return new(false, "نوع الخط الهدف غير محدد في طلب CGT.");
        }

        var eligibility = await _eligibility.ValidateForCreateAsync(
            entity.SubscriberProfileId,
            entity.MsisdnAssetId,
            entity.TargetSubscriptionTypeId,
            entity.GsmMigrationReason,
            cancellationToken);

        return new(eligibility.Allowed, eligibility.MessageAr);
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var targetTypeId = entity.TargetSubscriptionTypeId
            ?? throw new BusinessRuleViolationException("نوع الخط الهدف مطلوب.");

        var subsQuery = _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId);

        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            subsQuery = subsQuery.Where(s => s.MsisdnAssetId == entity.MsisdnAssetId);
        }

        var subscription = await subsQuery
            .OrderByDescending(s => s.IsPrimaryLine)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException("لا يوجد اشتراك لتحويل نوع الخط.");

        entity.SourceSubscriptionTypeId ??= subscription.SubscriptionTypeId;
        subscription.SubscriptionTypeId = targetTypeId;

        if (!string.IsNullOrEmpty(entity.ProductId))
        {
            subscription.ProductId = entity.ProductId;
        }

        subscription.UpdatedById = actorUserId;
        _subscriptionRepository.Update(subscription);

        return null;
    }
}
