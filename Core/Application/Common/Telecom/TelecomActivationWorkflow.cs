using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Application.Common.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Common.Telecom;

/// <summary>
/// Enterprise activation: DB transaction (reserve + bind) → CBS → domain notification → HLR/SMS handlers.
/// </summary>
public sealed class TelecomActivationWorkflow : ITelecomActivationWorkflow
{
    private const string IdempotentMessageAr = "الطلب معالج مسبقاً.";

    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly ISubscriptionBindingExecutor _bindingExecutor;
    private readonly ISubscriptionBindingCompensator _compensator;
    private readonly IBillingSystemIntegration _billing;
    private readonly IESimDpPlusService _eSimDpPlus;
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _query;
    private readonly ILogger<TelecomActivationWorkflow> _logger;

    public TelecomActivationWorkflow(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ITelecomOperationOrchestrator orchestrator,
        ISubscriptionBindingExecutor bindingExecutor,
        ISubscriptionBindingCompensator compensator,
        IBillingSystemIntegration billing,
        IESimDpPlusService eSimDpPlus,
        IPublisher publisher,
        IUnitOfWork unitOfWork,
        IQueryContext query,
        ILogger<TelecomActivationWorkflow> logger)
    {
        _operationRepository = operationRepository;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _subscriptionRepository = subscriptionRepository;
        _orchestrator = orchestrator;
        _bindingExecutor = bindingExecutor;
        _compensator = compensator;
        _billing = billing;
        _eSimDpPlus = eSimDpPlus;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
        _query = query;
        _logger = logger;
    }

    public async Task<TelecomActivationWorkflowResult> ConfirmActivationAsync(
        string operationId,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await _operationRepository.GetAsync(operationId, cancellationToken);
        if (entity == null)
        {
            return Fail(new TelecomOperationRequest(), "Operation not found.");
        }

        entity.CorrelationId ??= Guid.CreateVersion7().ToString();

        if (entity.Status == TelecomOperationStatus.Completed)
        {
            return new TelecomActivationWorkflowResult(
                entity,
                new BillingProvisionResult(true, IdempotentMessageAr),
                null,
                true,
                IdempotentMessageAr);
        }

        if (entity.DocumentStatus < TelecomDocumentStatus.Uploaded)
        {
            return Fail(entity, "يجب رفع الوثائق قبل التأكيد.");
        }

        if (entity.Kind == TelecomOperationKind.TakeOver)
        {
            if (string.IsNullOrWhiteSpace(entity.SecondarySubscriberProfileId))
            {
                return Fail(entity, "يجب تحديد المالك الجديد قبل اعتماد نقل الملكية.");
            }

            if (entity.SecondarySubscriberProfileId == entity.SubscriberProfileId)
            {
                return Fail(entity, "المالك الجديد يجب أن يختلف عن المالك الحالي.");
            }
        }

        var allowed = new[]
        {
            TelecomOperationStatus.Draft,
            TelecomOperationStatus.PendingDocuments,
            TelecomOperationStatus.PendingExternal
        };

        if (!allowed.Contains(entity.Status))
        {
            return Fail(entity, "حالة الطلب لا تسمح بالتأكيد.");
        }

        BindSubscriptionResult? bindResult = null;
        OperationProvisionContext? provisionCtx = null;
        BillingProvisionResult billingResult;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                if (entity.Status == TelecomOperationStatus.Draft)
                {
                    await _orchestrator.TransitionAsync(
                        entity, TelecomOperationStatus.PendingDocuments, actorUserId, "وثائق مكتملة", ct);
                }

                await _orchestrator.TransitionAsync(
                    entity, TelecomOperationStatus.Confirmed, actorUserId, "تأكيد محلي", ct);
                await _orchestrator.TransitionAsync(
                    entity, TelecomOperationStatus.Provisioning, actorUserId, "ربط محلي", ct);

                await EnsureMsisdnReservedForCustomerAsync(entity, actorUserId, ct);

                if (entity.Kind == TelecomOperationKind.NewActivation
                    && !string.IsNullOrEmpty(entity.MsisdnAssetId)
                    && !string.IsNullOrEmpty(entity.SimInventoryId))
                {
                    bindResult = await ApplyTripleBindAsync(entity, actorUserId, ct);
                    await ApplyESimActivationCodeIfNeededAsync(entity, bindResult.Msisdn, ct);
                }
                else if (entity.Kind == TelecomOperationKind.NewActivation && !string.IsNullOrEmpty(entity.MsisdnAssetId))
                {
                    await ApplyMsisdnOnlyActivationAsync(entity, actorUserId, ct);
                }
                else if (entity.Kind == TelecomOperationKind.SimSwap && !string.IsNullOrEmpty(entity.SimInventoryId))
                {
                    provisionCtx = await ApplySimSwapAsync(entity, actorUserId, ct);
                }
                else if (entity.Kind == TelecomOperationKind.Migration && !string.IsNullOrEmpty(entity.ProductId))
                {
                    await ApplyMigrationAsync(entity, actorUserId, ct);
                    provisionCtx = await BuildProvisionContextAsync(entity, ct);
                }
                else if (entity.Kind == TelecomOperationKind.TakeOver)
                {
                    provisionCtx = await ApplyTakeOverAsync(entity, actorUserId, ct);
                }

                _operationRepository.Update(entity);
                await _unitOfWork.SaveAsync(ct);
            }, cancellationToken);
        }
        catch (BusinessRuleViolationException ex)
        {
            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, ex.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            return Fail(entity, ex.Message);
        }
        catch (TelecomBindingRuleException ex)
        {
            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, ex.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            return Fail(entity, ex.Message);
        }

        var msisdn = bindResult?.Msisdn;
        if (string.IsNullOrEmpty(msisdn) && !string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
            msisdn = asset?.Msisdn;
        }

        var lineContext = await TelecomProvisionRequestBuilder.ResolveLineContextAsync(_query, entity, cancellationToken);
        if (string.IsNullOrEmpty(msisdn))
        {
            msisdn = lineContext.Msisdn;
        }

        var billingRequest = TelecomProvisionRequestBuilder.ToBillingRequest(entity, lineContext);

        billingResult = await _billing.ProvisionAsync(billingRequest, cancellationToken);
        if (!billingResult.Success)
        {
            if (bindResult != null)
            {
                await _compensator.CompensateAsync(
                    bindResult.TelecomSubscription.Id,
                    entity.MsisdnAssetId!,
                    entity.SimInventoryId!,
                    entity.SubscriberProfileId,
                    cancellationToken);
                await _billing.ReverseProvisionAsync(billingRequest, cancellationToken);
            }

            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, billingResult.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            return Fail(entity, billingResult.Message);
        }

        if (bindResult != null)
        {
            await PublishProvisionedAsync(
                entity,
                bindResult.Msisdn,
                bindResult.Iccid,
                bindResult.SubscriberProfile.CustomerId,
                bindResult.SubscriberProfile.Id,
                bindResult.TelecomSubscription.Id,
                actorUserId,
                cancellationToken);
        }
        else if (provisionCtx != null)
        {
            await PublishProvisionedAsync(
                entity,
                provisionCtx.Msisdn,
                provisionCtx.Iccid,
                provisionCtx.CustomerId,
                provisionCtx.SubscriberProfileId,
                provisionCtx.TelecomSubscriptionId,
                actorUserId,
                cancellationToken);
        }
        else if (RequiresNetworkProvision(entity.Kind))
        {
            var ctx = await BuildProvisionContextAsync(entity, cancellationToken);
            await PublishProvisionedAsync(
                entity,
                ctx.Msisdn,
                ctx.Iccid,
                ctx.CustomerId,
                ctx.SubscriberProfileId,
                ctx.TelecomSubscriptionId,
                actorUserId,
                cancellationToken);
        }
        else
        {
            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Completed, actorUserId, "اكتمل", cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new TelecomActivationWorkflowResult(
            entity,
            billingResult,
            null,
            false,
            billingResult.Message);
    }

    private async Task EnsureMsisdnReservedForCustomerAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(entity.MsisdnAssetId)) return;

        var customerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(customerId))
        {
            throw new BusinessRuleViolationException("ملف المشترك غير مرتبط بعميل.");
        }

        var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("رقم MSISDN غير موجود.");

        var utcNow = DateTime.UtcNow;
        asset.ReleaseReservationIfExpired(utcNow);

        if (asset.PoolStatus == MsisdnPoolStatus.Available)
        {
            asset.ReserveForCustomer(customerId, utcNow);
            asset.UpdatedById = actorUserId;
            _msisdnRepository.Update(asset);
        }
        else if (asset.PoolStatus == MsisdnPoolStatus.Reserved && asset.ReservedForCustomerId != customerId)
        {
            throw new BusinessRuleViolationException("الرقم محجوز لعميل آخر.");
        }
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

        var customerKind = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.Customer!.CustomerKind)
            .FirstOrDefaultAsync(cancellationToken);

        var subscriptionTypeId = customerKind == CustomerKind.Corporate
            ? TelecomSubscriptionTypeWellKnownIds.Postpaid
            : TelecomSubscriptionTypeWellKnownIds.Prepaid;

        return await _bindingExecutor.ExecuteAsync(
            new BindSubscriptionCommand(
                customerId,
                entity.SubscriberProfileId,
                entity.MsisdnAssetId!,
                entity.SimInventoryId!,
                entity.ProductOfferingId ?? string.Empty,
                entity.Id,
                entity.CorrelationId),
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
        if (string.IsNullOrEmpty(entity.SimInventoryId)) return;

        var sim = await _simRepository.GetAsync(entity.SimInventoryId, cancellationToken);
        if (sim == null || !sim.IsESim || string.IsNullOrEmpty(sim.Eid)) return;

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
        if (msisdnAsset == null) return;

        msisdnAsset.SubscriberProfileId = entity.SubscriberProfileId;
        msisdnAsset.TransitionTo(MsisdnPoolStatus.Active);
        _msisdnRepository.Update(msisdnAsset);

        var customerKind = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.Customer!.CustomerKind)
            .FirstOrDefaultAsync(cancellationToken);

        await _subscriptionRepository.CreateAsync(new TelecomSubscription
        {
            SubscriberProfileId = entity.SubscriberProfileId,
            MsisdnAssetId = entity.MsisdnAssetId!,
            ProductId = entity.ProductId,
            SubscriptionTypeId = customerKind == CustomerKind.Corporate
                ? TelecomSubscriptionTypeWellKnownIds.Postpaid
                : TelecomSubscriptionTypeWellKnownIds.Prepaid,
            DocumentStatus = entity.DocumentStatus,
            IsPrimaryLine = false,
            CreatedById = actorUserId
        }, cancellationToken);
    }

    private async Task<OperationProvisionContext> ApplySimSwapAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var newSim = await _simRepository.GetAsync(entity.SimInventoryId!, cancellationToken)
            ?? throw new BusinessRuleViolationException("الشريحة الجديدة غير موجودة.");

        if (newSim.Status is not (SimStatus.Available or SimStatus.Reserved))
        {
            throw new BusinessRuleViolationException("الشريحة الجديدة غير متاحة للتبديل.");
        }

        var oldSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == entity.SubscriberProfileId
                && s.Status == SimStatus.Active
                && s.Id != entity.SimInventoryId)
            .ToListAsync(cancellationToken);

        foreach (var old in oldSims)
        {
            old.TransitionTo(SimStatus.Quarantined);
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

        return await BuildProvisionContextAsync(entity, cancellationToken, newSim.Iccid);
    }

    private async Task<OperationProvisionContext> BuildProvisionContextAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken,
        string? iccidOverride = null)
    {
        string? msisdn = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
            msisdn = asset?.Msisdn;
        }

        string? iccid = iccidOverride;
        if (string.IsNullOrEmpty(iccid) && !string.IsNullOrEmpty(entity.SimInventoryId))
        {
            var sim = await _simRepository.GetAsync(entity.SimInventoryId, cancellationToken);
            iccid = sim?.Iccid;
        }

        if (string.IsNullOrEmpty(iccid))
        {
            iccid = await _simRepository.GetQuery()
                .Where(s => !s.IsDeleted
                    && s.SubscriberProfileId == entity.SubscriberProfileId
                    && s.Status == SimStatus.Active)
                .Select(s => s.Iccid)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var sub = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId)
            .OrderByDescending(s => s.IsPrimaryLine)
            .FirstOrDefaultAsync(cancellationToken);

        var customerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == entity.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationProvisionContext(
            msisdn,
            iccid,
            customerId,
            entity.SubscriberProfileId,
            sub?.Id,
            entity.MsisdnAssetId,
            entity.SimInventoryId);
    }

    private async Task PublishProvisionedAsync(
        TelecomOperationRequest entity,
        string? msisdn,
        string? iccid,
        string? customerId,
        string? subscriberProfileId,
        string? telecomSubscriptionId,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        await _publisher.Publish(
            new TelecomOperationProvisionedNotification(
                entity.Kind,
                entity.Id,
                msisdn,
                iccid,
                entity.CorrelationId,
                actorUserId,
                customerId,
                subscriberProfileId,
                telecomSubscriptionId,
                entity.MsisdnAssetId,
                entity.SimInventoryId),
            cancellationToken);
    }

    private static bool RequiresNetworkProvision(TelecomOperationKind kind) =>
        kind is TelecomOperationKind.NewActivation
            or TelecomOperationKind.SimSwap
            or TelecomOperationKind.Migration
            or TelecomOperationKind.TakeOver;

    private async Task<OperationProvisionContext> ApplyTakeOverAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var newProfileId = entity.SecondarySubscriberProfileId
            ?? throw new BusinessRuleViolationException("يجب تحديد المالك الجديد.");

        if (newProfileId == entity.SubscriberProfileId)
        {
            throw new BusinessRuleViolationException("المالك الجديد يجب أن يختلف عن المالك الحالي.");
        }

        var newCustomerId = await _profileRepository.GetQuery()
            .Where(p => p.Id == newProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المالك الجديد غير موجود.");

        string? msisdn = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var msisdnAsset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken)
                ?? throw new BusinessRuleViolationException("رقم الخط غير موجود.");

            if (msisdnAsset.PoolStatus != MsisdnPoolStatus.Active)
            {
                throw new BusinessRuleViolationException(
                    $"لا يمكن نقل ملكية الرقم {msisdnAsset.Msisdn} لأن حالته {msisdnAsset.PoolStatus}.");
            }

            msisdn = msisdnAsset.Msisdn;
            msisdnAsset.SubscriberProfileId = newProfileId;
            msisdnAsset.ReservedForCustomerId = newCustomerId;
            msisdnAsset.UpdatedById = actorUserId;
            _msisdnRepository.Update(msisdnAsset);
        }

        var subsQuery = _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == entity.SubscriberProfileId);

        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            subsQuery = subsQuery.Where(s => s.MsisdnAssetId == entity.MsisdnAssetId);
        }

        var subs = await subsQuery.ToListAsync(cancellationToken);
        foreach (var sub in subs)
        {
            sub.SubscriberProfileId = newProfileId;
            sub.DocumentStatus = TelecomDocumentStatus.Verified;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
        }

        var activeSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == entity.SubscriberProfileId
                && s.Status == SimStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var sim in activeSims)
        {
            sim.AssignToProfile(newProfileId);
            sim.UpdatedById = actorUserId;
            _simRepository.Update(sim);
        }

        entity.DocumentStatus = TelecomDocumentStatus.Verified;
        entity.UpdatedById = actorUserId;

        var primarySub = subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs.FirstOrDefault();
        string? iccid = null;
        if (!string.IsNullOrEmpty(entity.SimInventoryId))
        {
            var sim = await _simRepository.GetAsync(entity.SimInventoryId, cancellationToken);
            iccid = sim?.Iccid;
        }

        if (string.IsNullOrEmpty(iccid) && activeSims.Count > 0)
        {
            iccid = activeSims[0].Iccid;
        }

        return new OperationProvisionContext(
            msisdn,
            iccid,
            newCustomerId,
            newProfileId,
            primarySub?.Id,
            entity.MsisdnAssetId,
            entity.SimInventoryId);
    }

    private sealed record OperationProvisionContext(
        string? Msisdn,
        string? Iccid,
        string? CustomerId,
        string? SubscriberProfileId,
        string? TelecomSubscriptionId,
        string? MsisdnAssetId,
        string? SimInventoryId);

    private async Task ApplyMigrationAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var activeSub = await _subscriptionRepository.GetQuery()
            .Where(s => s.SubscriberProfileId == entity.SubscriberProfileId && s.IsPrimaryLine)
            .FirstOrDefaultAsync(cancellationToken);
        if (activeSub != null)
        {
            activeSub.ProductId = entity.ProductId;
            activeSub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(activeSub);
        }
    }

    private static TelecomActivationWorkflowResult Fail(TelecomOperationRequest entity, string message) =>
        new(entity, new BillingProvisionResult(false, message), null, false, message);
}
