using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom;

public sealed record HlrFailureCompensationResult(
    bool CbsReversed,
    bool LocalBindCompensated,
    string MessageAr);

/// <summary>Reverses CBS + local bind when HLR hard-fails after CBS succeeded (Ghost Profile mitigation).</summary>
public interface ITelecomHlrFailureCompensator
{
    Task<HlrFailureCompensationResult> CompensateAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        string? actorUserId,
        string hlrErrorMessage,
        CancellationToken cancellationToken);
}

public sealed class TelecomHlrFailureCompensator : ITelecomHlrFailureCompensator
{
    private const string CompensationMessageAr =
        "فشل تزويد الشبكة (HLR) — تم عكس الحساب المالي (CBS) وإلغاء الربط المحلي.";

    private readonly IBillingSystemIntegration _billing;
    private readonly ISubscriptionBindingCompensator _bindingCompensator;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TelecomHlrFailureCompensator(
        IBillingSystemIntegration billing,
        ISubscriptionBindingCompensator bindingCompensator,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _billing = billing;
        _bindingCompensator = bindingCompensator;
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<HlrFailureCompensationResult> CompensateAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        string? actorUserId,
        string hlrErrorMessage,
        CancellationToken cancellationToken)
    {
        var billingRequest = TelecomProvisionRequestBuilder.ToBillingRequest(
            operation,
            lineContext,
            TelecomBillingProvisionPhase.Reverse);

        var reverse = await _billing.ReverseProvisionAsync(billingRequest, cancellationToken);
        var cbsReversed = reverse.Success;

        var localCompensated = false;
        if (operation.Kind == TelecomOperationKind.NewActivation
            && !string.IsNullOrEmpty(operation.MsisdnAssetId)
            && !string.IsNullOrEmpty(operation.SimInventoryId))
        {
            var subId = await _subscriptionRepository.GetQuery()
                .Where(s => !s.IsDeleted
                    && s.MsisdnAssetId == operation.MsisdnAssetId
                    && s.SubscriberProfileId == operation.SubscriberProfileId)
                .OrderByDescending(s => s.CreatedAtUtc)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrEmpty(subId))
            {
                await _bindingCompensator.CompensateAsync(
                    subId,
                    operation.MsisdnAssetId,
                    operation.SimInventoryId,
                    operation.SubscriberProfileId,
                    cancellationToken);
                localCompensated = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }

        var msg = $"{CompensationMessageAr} ({hlrErrorMessage})";
        return new HlrFailureCompensationResult(cbsReversed, localCompensated, msg);
    }
}
