using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OperationConfirm;

public sealed class NewActivationConfirmStrategy : IOperationConfirmStrategy
{
    private readonly ISubscriptionBindingExecutor _bindingExecutor;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly IESimDpPlusService _eSimDpPlus;
    private readonly IQueryContext _query;

    public NewActivationConfirmStrategy(
        ISubscriptionBindingExecutor bindingExecutor,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        IESimDpPlusService eSimDpPlus,
        IQueryContext query)
    {
        _bindingExecutor = bindingExecutor;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _subscriptionRepository = subscriptionRepository;
        _eSimDpPlus = eSimDpPlus;
        _query = query;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.NewActivation;

    public bool RequiresNetworkProvision => true;

    public async Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (!OperationConfirmValidationHelpers.HasKycProofForActivation(entity))
        {
            return new(false, TelecomUserMessages.ValAct12KycRequired.Ar);
        }

        var requiredDeposit = await ResolveRequiredDepositAsync(entity, cancellationToken);
        if (requiredDeposit > 0 && string.IsNullOrWhiteSpace(entity.PaymentReference))
        {
            return new(false, "يجب تسجيل الدفع (PaymentReference) قبل تأكيد التفعيل.");
        }

        return new(true, "جاهز للتفعيل.");
    }

    public async Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(entity.SimInventoryId))
        {
            var bind = await ApplyTripleBindAsync(entity, actorUserId, cancellationToken);
            await ApplyESimActivationCodeIfNeededAsync(entity, bind.Msisdn, cancellationToken);
            return new OperationApplyResult(
                bind.Msisdn,
                bind.Iccid,
                bind.SubscriberProfile.CustomerId,
                bind.SubscriberProfile.Id,
                bind.TelecomSubscription.Id);
        }

        await ApplyMsisdnOnlyActivationAsync(entity, actorUserId, cancellationToken);
        var ctx = await BuildMsisdnOnlyResultAsync(entity, cancellationToken);
        return ctx;
    }

    private async Task<BindSubscriptionResult> ApplyTripleBindAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var customerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير مرتبط بعميل.");

        var subscriptionTypeId = await ResolveActivationSubscriptionTypeIdAsync(entity, cancellationToken);

        return await _bindingExecutor.ExecuteAsync(
            new BindSubscriptionCommand(
                customerId,
                entity.SubscriberProfileId,
                entity.MsisdnAssetId!,
                entity.SimInventoryId!,
                entity.ProductOfferingId ?? string.Empty,
                entity.Id,
                entity.CorrelationId,
                entity.DocumentStatus),
            subscriptionTypeId,
            actorUserId,
            requireStrictReservation: true,
            cancellationToken);
    }

    private async Task ApplyESimActivationCodeIfNeededAsync(
        TelecomOperationRequest entity,
        string msisdn,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.SimInventoryId))
        {
            return;
        }

        var sim = await _simRepository.GetAsync(entity.SimInventoryId, cancellationToken);
        if (sim == null || !sim.IsESim || string.IsNullOrEmpty(sim.Eid))
        {
            return;
        }

        var dpResult = await _eSimDpPlus.RequestActivationCodeAsync(
            new ESimActivationCodeRequest(sim.Eid, msisdn, entity.CorrelationId),
            cancellationToken);

        if (dpResult.Success && !string.IsNullOrEmpty(dpResult.ActivationCode))
        {
            sim.SetActivationCodeFromDpPlus(dpResult.ActivationCode);
            _simRepository.Update(sim);
        }
    }

    private async Task ApplyMsisdnOnlyActivationAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var msisdnAsset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId!, cancellationToken);
        if (msisdnAsset == null)
        {
            return;
        }

        msisdnAsset.SubscriberProfileId = entity.SubscriberProfileId;
        msisdnAsset.TransitionTo(MsisdnPoolStatus.Active);
        _msisdnRepository.Update(msisdnAsset);

        var subscriptionTypeId = await ResolveActivationSubscriptionTypeIdAsync(entity, cancellationToken);

        await _subscriptionRepository.CreateAsync(new TelecomSubscription
        {
            SubscriberProfileId = entity.SubscriberProfileId,
            MsisdnAssetId = entity.MsisdnAssetId!,
            ProductId = entity.ProductId,
            SubscriptionTypeId = subscriptionTypeId,
            DocumentStatus = entity.DocumentStatus,
            IsPrimaryLine = false,
            CreatedById = actorUserId
        }, cancellationToken);
    }

    private async Task<OperationApplyResult?> BuildMsisdnOnlyResultAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken)
    {
        var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId!, cancellationToken);
        var customerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        var subId = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId && s.MsisdnAssetId == entity.MsisdnAssetId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationApplyResult(asset?.Msisdn, null, customerId, entity.SubscriberProfileId, subId);
    }

    private async Task<decimal> ResolveRequiredDepositAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken)
    {
        if (entity.InitialDepositAmount is > 0)
        {
            return entity.InitialDepositAmount.Value;
        }

        if (string.IsNullOrEmpty(entity.ProductId))
        {
            return 0m;
        }

        var unitPrice = await _query.Product.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == entity.ProductId)
            .Select(p => p.UnitPrice)
            .FirstOrDefaultAsync(cancellationToken);

        return unitPrice is > 0 ? (decimal)unitPrice : 0m;
    }

    private async Task<string> ResolveActivationSubscriptionTypeIdAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var intended = await _msisdnRepository.GetQuery()
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == entity.MsisdnAssetId)
                .Select(m => m.IntendedSubscriptionTypeId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrEmpty(intended))
            {
                return intended;
            }
        }

        var customerKind = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.Customer!.CustomerKind)
            .FirstOrDefaultAsync(cancellationToken);

        return customerKind == CustomerKind.Corporate
            ? TelecomSubscriptionTypeWellKnownIds.Postpaid
            : TelecomSubscriptionTypeWellKnownIds.Prepaid;
    }
}
