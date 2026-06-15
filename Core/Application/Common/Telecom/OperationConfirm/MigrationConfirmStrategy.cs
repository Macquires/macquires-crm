using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.OfferSubscription;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class MigrationConfirmStrategy : IOperationConfirmStrategy
{
    private readonly IOfferSubscriptionEligibilityChecker _eligibility;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;

    public MigrationConfirmStrategy(
        IOfferSubscriptionEligibilityChecker eligibility,
        ICommandRepository<TelecomSubscription> subscriptionRepository)
    {
        _eligibility = eligibility;
        _subscriptionRepository = subscriptionRepository;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.Migration;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return new(false, "رقم الخط غير محدد في طلب ترحيل الباقة.");
        }

        var check = await _eligibility.ValidateForConfirmAsync(entity, cancellationToken);
        if (!check.Allowed)
        {
            return new(false, check.MessageAr);
        }

        entity.PriorProductId ??= check.PriorProductId;
        entity.PriorProductOfferingId ??= check.PriorProductOfferingId;
        return new(true, check.MessageAr);
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.ProductId))
        {
            return null;
        }

        var subsQuery = _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId);

        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            subsQuery = subsQuery.Where(s => s.MsisdnAssetId == entity.MsisdnAssetId);
        }

        var subscription = await subsQuery
            .OrderByDescending(s => s.IsPrimaryLine)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            return null;
        }

        entity.PriorProductId ??= subscription.ProductId;
        entity.PriorProductOfferingId ??= subscription.ProductOfferingId;
        subscription.ProductId = entity.ProductId;
        subscription.ProductOfferingId = entity.ProductOfferingId;
        subscription.UpdatedById = actorUserId;
        _subscriptionRepository.Update(subscription);

        return null;
    }
}
