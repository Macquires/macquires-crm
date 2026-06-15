using Application.Common.CQS.Queries;
using Application.Common.Events;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Settings;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Billing;
using Application.Common.Telecom.DeviceSales;
using Application.Common.Telecom.OperationConfirm;
using Application.Common.Telecom.Refund;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.Activation;

public interface ITelecomActivationProvisioningExecutor
{
    Task<TelecomActivationWorkflowResult> ExecuteAsync(
        TelecomOperationRequest entity,
        IOperationConfirmStrategy confirmStrategy,
        string? actorUserId,
        CancellationToken cancellationToken);
}

public sealed class TelecomActivationProvisioningExecutor : ITelecomActivationProvisioningExecutor
{
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly ISubscriptionBindingCompensator _compensator;
    private readonly IBillingRoutingOrchestrator _billingRouting;
    private readonly ITelecomProvisionedEventDispatcher _provisionedDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _query;
    private readonly ITechnicalTicketQueueIngestionService _ticketQueue;
    private readonly ITelecomHlrFailureCompensator _hlrFailureCompensator;
    private readonly IDeviceSaleCompletionService _deviceSaleCompletion;
    private readonly IRefundCompletionService _refundCompletion;
    private readonly IBadDebtCompletionService _badDebtCompletion;
    private readonly IOperationConfirmProvisionContextBuilder _provisionContextBuilder;
    private readonly IGlobalSettingsProvider _globalSettings;
    private readonly ITelecomActivationMsisdnReservationService _msisdnReservation;

    public TelecomActivationProvisioningExecutor(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ITelecomOperationOrchestrator orchestrator,
        ISubscriptionBindingCompensator compensator,
        IBillingRoutingOrchestrator billingRouting,
        ITelecomProvisionedEventDispatcher provisionedDispatcher,
        IUnitOfWork unitOfWork,
        IQueryContext query,
        ITechnicalTicketQueueIngestionService ticketQueue,
        ITelecomHlrFailureCompensator hlrFailureCompensator,
        IDeviceSaleCompletionService deviceSaleCompletion,
        IRefundCompletionService refundCompletion,
        IBadDebtCompletionService badDebtCompletion,
        IOperationConfirmProvisionContextBuilder provisionContextBuilder,
        IGlobalSettingsProvider globalSettings,
        ITelecomActivationMsisdnReservationService msisdnReservation)
    {
        _operationRepository = operationRepository;
        _msisdnRepository = msisdnRepository;
        _orchestrator = orchestrator;
        _compensator = compensator;
        _billingRouting = billingRouting;
        _provisionedDispatcher = provisionedDispatcher;
        _unitOfWork = unitOfWork;
        _query = query;
        _ticketQueue = ticketQueue;
        _hlrFailureCompensator = hlrFailureCompensator;
        _deviceSaleCompletion = deviceSaleCompletion;
        _refundCompletion = refundCompletion;
        _badDebtCompletion = badDebtCompletion;
        _provisionContextBuilder = provisionContextBuilder;
        _globalSettings = globalSettings;
        _msisdnReservation = msisdnReservation;
    }

    public async Task<TelecomActivationWorkflowResult> ExecuteAsync(
        TelecomOperationRequest entity,
        IOperationConfirmStrategy confirmStrategy,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var fromScheduled = entity.Status == TelecomOperationStatus.Scheduled;
        OperationApplyResult? applyResult = null;
        var deviceSaleFulfilled = false;
        var refundFulfilled = false;
        var badDebtFulfilled = false;
        var vasFulfilled = false;
        OperationProvisionContext? provisionCtx = null;
        BillingProvisionResult billingResult;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                if (!fromScheduled)
                {
                    if (entity.Status == TelecomOperationStatus.Draft)
                    {
                        await _orchestrator.TransitionAsync(
                            entity, TelecomOperationStatus.PendingDocuments, actorUserId, "وثائق مكتملة", ct);
                    }

                    await _orchestrator.TransitionAsync(
                        entity, TelecomOperationStatus.Confirmed, actorUserId, "تأكيد محلي", ct);
                }

                await _orchestrator.TransitionAsync(
                    entity, TelecomOperationStatus.Provisioning, actorUserId, "ربط محلي", ct);

                await _msisdnReservation.EnsureReservedForCustomerAsync(entity, actorUserId, ct);

                applyResult = await confirmStrategy.ApplyLocalChangesAsync(entity, actorUserId, ct);

                if (applyResult != null && TelecomActivationWorkflowSupport.HasProvisionPayload(applyResult))
                {
                    provisionCtx = TelecomActivationWorkflowSupport.ToProvisionContext(applyResult, entity);
                }
                else if (confirmStrategy.RequiresNetworkProvision)
                {
                    provisionCtx = await _provisionContextBuilder.BuildAsync(entity, ct);
                }

                deviceSaleFulfilled = entity.Kind == TelecomOperationKind.DeviceSale;
                refundFulfilled = entity.Kind == TelecomOperationKind.DepositRefundSettlement;
                badDebtFulfilled = entity.Kind == TelecomOperationKind.BadDebtRecovery;
                vasFulfilled = entity.Kind == TelecomOperationKind.ServiceModification;

                _operationRepository.Update(entity);
                await _unitOfWork.SaveAsync(ct);
            }, cancellationToken);
        }
        catch (Exception ex) when (entity.Kind is TelecomOperationKind.DeviceSale
                                   or TelecomOperationKind.DepositRefundSettlement
                                   or TelecomOperationKind.BadDebtRecovery)
        {
            if (entity.Kind == TelecomOperationKind.DeviceSale)
            {
                await _deviceSaleCompletion.CompensateOnFailureAsync(entity, cancellationToken);
            }
            else if (entity.Kind == TelecomOperationKind.DepositRefundSettlement)
            {
                await _refundCompletion.CompensateOnFailureAsync(entity, cancellationToken);
            }
            else
            {
                await _badDebtCompletion.CompensateOnFailureAsync(entity, cancellationToken);
            }

            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, ex.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            await _ticketQueue.EnqueueProvisioningFalloutAsync(entity, ex.Message, actorUserId, cancellationToken);
            return TelecomActivationWorkflowSupport.Fail(entity, ex.Message);
        }
        catch (BusinessRuleViolationException ex)
        {
            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, ex.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            return TelecomActivationWorkflowSupport.Fail(entity, ex.Message);
        }
        catch (TelecomBindingRuleException ex)
        {
            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, ex.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            return TelecomActivationWorkflowSupport.Fail(entity, ex.Message);
        }

        var msisdn = applyResult?.Msisdn;
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

        if (deviceSaleFulfilled)
        {
            billingResult = new BillingProvisionResult(true, "اكتمل بيع الجهاز (CBS).");
        }
        else if (refundFulfilled)
        {
            billingResult = new BillingProvisionResult(true, "اكتمل استرداد المبلغ (CBS + محفظة).");
        }
        else if (badDebtFulfilled)
        {
            billingResult = new BillingProvisionResult(true, "اكتمل ترحيل التحصيل (CBS).");
        }
        else if (vasFulfilled)
        {
            billingResult = new BillingProvisionResult(true, "اكتمل تعديل خدمة VAS (HLR + محلي).");
        }
        else
        {
            billingResult = await _billingRouting.ProvisionAsync(
                entity,
                lineContext,
                billingRequest,
                cancellationToken);
        }

        if (!billingResult.Success)
        {
            await CompensateBillingFailureAsync(
                entity,
                applyResult,
                lineContext,
                billingRequest,
                actorUserId,
                billingResult.Message,
                cancellationToken);

            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, billingResult.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            var falloutSource = _billingRouting.IntegrationFalloutSource(entity.Kind, lineContext.SubscriptionTypeCode);
            await _ticketQueue.EnqueueProvisioningFalloutAsync(
                entity, $"{falloutSource}: {billingResult.Message}", actorUserId, cancellationToken);
            return TelecomActivationWorkflowSupport.Fail(entity, billingResult.Message);
        }

        if (entity.Kind == TelecomOperationKind.Termination)
        {
            var defaultFinalBill = await _globalSettings.GetDecimalAsync(
                GlobalSettingKeys.TelecomTerminationDefaultFinalBillAmountSyp,
                defaultValue: 12_500m,
                min: 0m,
                max: 10_000_000m,
                cancellationToken);
            entity.FinalBillAmount ??= defaultFinalBill;
            entity.DepositSettlementAmount ??= 0m;
            entity.DepositSettlementStatus ??= "Settled";
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        if (provisionCtx != null)
        {
            await PublishProvisionedAsync(entity, provisionCtx, actorUserId, cancellationToken);
        }
        else if (confirmStrategy.RequiresNetworkProvision)
        {
            var ctx = await _provisionContextBuilder.BuildAsync(entity, cancellationToken);
            await PublishProvisionedAsync(entity, ctx, actorUserId, cancellationToken);
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

    private async Task CompensateBillingFailureAsync(
        TelecomOperationRequest entity,
        OperationApplyResult? applyResult,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        string? actorUserId,
        string message,
        CancellationToken cancellationToken)
    {
        if (entity.Kind == TelecomOperationKind.DeviceSale)
        {
            await _deviceSaleCompletion.CompensateOnFailureAsync(entity, cancellationToken);
        }
        else if (entity.Kind == TelecomOperationKind.DepositRefundSettlement)
        {
            await _refundCompletion.CompensateOnFailureAsync(entity, cancellationToken);
        }
        else if (entity.Kind == TelecomOperationKind.BadDebtRecovery)
        {
            await _badDebtCompletion.CompensateOnFailureAsync(entity, cancellationToken);
        }

        if (applyResult?.TelecomSubscriptionId != null
            && entity.Kind == TelecomOperationKind.NewActivation
            && !string.IsNullOrEmpty(entity.MsisdnAssetId)
            && !string.IsNullOrEmpty(entity.SimInventoryId))
        {
            await _compensator.CompensateAsync(
                applyResult.TelecomSubscriptionId,
                entity.MsisdnAssetId!,
                entity.SimInventoryId!,
                entity.SubscriberProfileId,
                cancellationToken);

            await _billingRouting.CompensateProvisionAsync(
                entity,
                lineContext,
                billingRequest,
                cancellationToken);
        }
        else if (entity.Kind is TelecomOperationKind.NumberPortability
                 or TelecomOperationKind.Termination
                 or TelecomOperationKind.TemporarySuspension
                 or TelecomOperationKind.Reconnect)
        {
            await _hlrFailureCompensator.CompensateAsync(
                entity,
                lineContext,
                actorUserId,
                message,
                cancellationToken);
        }
    }

    private async Task PublishProvisionedAsync(
        TelecomOperationRequest entity,
        OperationProvisionContext ctx,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        await _provisionedDispatcher.DispatchAsync(
            new TelecomOperationProvisionedNotification(
                entity.Kind,
                entity.Id,
                ctx.Msisdn,
                ctx.Iccid,
                entity.CorrelationId,
                actorUserId,
                ctx.CustomerId,
                ctx.SubscriberProfileId,
                ctx.TelecomSubscriptionId,
                entity.MsisdnAssetId,
                entity.SimInventoryId),
            cancellationToken);
    }
}
