using Application.Common;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Application.Common.Events;
using Application.Common.Telecom.ChangeGsm;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.SimSwap;
using Application.Common.Telecom.TakeOver;
using Application.Common.Telecom.Termination;
using Application.Common.Telecom.Suspension;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.DeviceSales;
using Application.Common.Telecom.Refund;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.OfferSubscription;
using Application.Common.Telecom.Billing;
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
    private readonly IBillingRoutingOrchestrator _billingRouting;
    private readonly IESimDpPlusService _eSimDpPlus;
    private readonly ITelecomProvisionedEventDispatcher _provisionedDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _query;
    private readonly ILogger<TelecomActivationWorkflow> _logger;
    private readonly ITechnicalTicketQueueIngestionService _ticketQueue;
    private readonly IChangeGsmEligibilityChecker _changeGsmEligibility;
    private readonly ITakeOverEligibilityChecker _takeOverEligibility;
    private readonly ISimSwapEligibilityChecker _simSwapEligibility;
    private readonly IChangeNumberEligibilityChecker _changeNumberEligibility;
    private readonly ITerminationEligibilityChecker _terminationEligibility;
    private readonly ISuspensionEligibilityChecker _suspensionEligibility;
    private readonly IReconnectEligibilityChecker _reconnectEligibility;
    private readonly IOfferSubscriptionEligibilityChecker _offerSubscriptionEligibility;
    private readonly ITelecomHlrFailureCompensator _hlrFailureCompensator;
    private readonly IDeviceSalesEligibilityChecker _deviceSalesEligibility;
    private readonly IDeviceSaleCompletionService _deviceSaleCompletion;
    private readonly IRefundEligibilityChecker _refundEligibility;
    private readonly IRefundCompletionService _refundCompletion;
    private readonly IBadDebtEligibilityChecker _badDebtEligibility;
    private readonly IBadDebtCompletionService _badDebtCompletion;
    private readonly ITelecomInventoryRulesProvider _inventoryRules;

    public TelecomActivationWorkflow(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ITelecomOperationOrchestrator orchestrator,
        ISubscriptionBindingExecutor bindingExecutor,
        ISubscriptionBindingCompensator compensator,
        IBillingRoutingOrchestrator billingRouting,
        IESimDpPlusService eSimDpPlus,
        ITelecomProvisionedEventDispatcher provisionedDispatcher,
        IUnitOfWork unitOfWork,
        IQueryContext query,
        ILogger<TelecomActivationWorkflow> logger,
        ITechnicalTicketQueueIngestionService ticketQueue,
        IChangeGsmEligibilityChecker changeGsmEligibility,
        ITakeOverEligibilityChecker takeOverEligibility,
        ISimSwapEligibilityChecker simSwapEligibility,
        IChangeNumberEligibilityChecker changeNumberEligibility,
        ITerminationEligibilityChecker terminationEligibility,
        ISuspensionEligibilityChecker suspensionEligibility,
        IReconnectEligibilityChecker reconnectEligibility,
        IOfferSubscriptionEligibilityChecker offerSubscriptionEligibility,
        ITelecomHlrFailureCompensator hlrFailureCompensator,
        IDeviceSalesEligibilityChecker deviceSalesEligibility,
        IDeviceSaleCompletionService deviceSaleCompletion,
        IRefundEligibilityChecker refundEligibility,
        IRefundCompletionService refundCompletion,
        IBadDebtEligibilityChecker badDebtEligibility,
        IBadDebtCompletionService badDebtCompletion,
        ITelecomInventoryRulesProvider inventoryRules)
    {
        _operationRepository = operationRepository;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _subscriptionRepository = subscriptionRepository;
        _orchestrator = orchestrator;
        _bindingExecutor = bindingExecutor;
        _compensator = compensator;
        _billingRouting = billingRouting;
        _eSimDpPlus = eSimDpPlus;
        _provisionedDispatcher = provisionedDispatcher;
        _unitOfWork = unitOfWork;
        _query = query;
        _logger = logger;
        _ticketQueue = ticketQueue;
        _changeGsmEligibility = changeGsmEligibility;
        _takeOverEligibility = takeOverEligibility;
        _simSwapEligibility = simSwapEligibility;
        _changeNumberEligibility = changeNumberEligibility;
        _terminationEligibility = terminationEligibility;
        _suspensionEligibility = suspensionEligibility;
        _reconnectEligibility = reconnectEligibility;
        _offerSubscriptionEligibility = offerSubscriptionEligibility;
        _hlrFailureCompensator = hlrFailureCompensator;
        _deviceSalesEligibility = deviceSalesEligibility;
        _deviceSaleCompletion = deviceSaleCompletion;
        _refundEligibility = refundEligibility;
        _refundCompletion = refundCompletion;
        _badDebtEligibility = badDebtEligibility;
        _badDebtCompletion = badDebtCompletion;
        _inventoryRules = inventoryRules;
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

        if (entity.Kind == TelecomOperationKind.NewActivation)
        {
            if (!HasKycProofForActivation(entity))
            {
                return Fail(entity, TelecomUserMessages.ValAct12KycRequired);
            }

            var requiredDeposit = await ResolveRequiredDepositAsync(entity, cancellationToken);
            if (requiredDeposit > 0 && string.IsNullOrWhiteSpace(entity.PaymentReference))
            {
                return Fail(entity, "يجب تسجيل الدفع (PaymentReference) قبل تأكيد التفعيل.");
            }
        }

        if (entity.Kind == TelecomOperationKind.ChangeGsmType)
        {
            if (string.IsNullOrEmpty(entity.TargetSubscriptionTypeId))
            {
                return Fail(entity, "نوع الخط الهدف غير محدد في طلب CGT.");
            }

            var eligibility = await _changeGsmEligibility.ValidateForCreateAsync(
                entity.SubscriberProfileId,
                entity.MsisdnAssetId,
                entity.TargetSubscriptionTypeId,
                entity.GsmMigrationReason,
                cancellationToken);

            if (!eligibility.Allowed)
            {
                return Fail(entity, eligibility.MessageAr);
            }
        }

        if (entity.Kind == TelecomOperationKind.TakeOver)
        {
            var takeOverCheck = await _takeOverEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!takeOverCheck.Allowed)
            {
                return Fail(entity, takeOverCheck.MessageAr);
            }
        }

        if (entity.Kind == TelecomOperationKind.SimSwap)
        {
            if (string.IsNullOrEmpty(entity.SimInventoryId))
            {
                return Fail(entity, "الشريحة الجديدة غير محددة في الطلب.");
            }

            entity.PriorSimInventoryId ??= await ResolvePriorSimIdForSwapAsync(entity, cancellationToken);

            var simSwapCheck = await _simSwapEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!simSwapCheck.Allowed)
            {
                return Fail(entity, simSwapCheck.MessageAr);
            }

            if (!string.IsNullOrEmpty(simSwapCheck.PriorSimInventoryId))
            {
                entity.PriorSimInventoryId = simSwapCheck.PriorSimInventoryId;
            }
        }

        if (entity.Kind == TelecomOperationKind.NumberPortability)
        {
            if (string.IsNullOrEmpty(entity.TargetMsisdnAssetId))
            {
                return Fail(entity, "الرقم المستهدف غير محدد في الطلب.");
            }

            entity.PriorMsisdnAssetId ??= entity.MsisdnAssetId;

            var changeNumberCheck = await _changeNumberEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!changeNumberCheck.Allowed)
            {
                return Fail(entity, changeNumberCheck.MessageAr);
            }
        }

        if (entity.Kind == TelecomOperationKind.Termination)
        {
            if (string.IsNullOrEmpty(entity.MsisdnAssetId))
            {
                return Fail(entity, "رقم الخط غير محدد في طلب الإنهاء.");
            }

            entity.PriorMsisdnAssetId ??= entity.MsisdnAssetId;

            var terminationCheck = await _terminationEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!terminationCheck.Allowed)
            {
                return Fail(entity, terminationCheck.MessageAr);
            }

            if (!string.IsNullOrEmpty(terminationCheck.PriorSimInventoryId))
            {
                entity.PriorSimInventoryId ??= terminationCheck.PriorSimInventoryId;
            }
        }

        if (entity.Kind == TelecomOperationKind.Migration)
        {
            if (string.IsNullOrEmpty(entity.MsisdnAssetId))
            {
                return Fail(entity, "رقم الخط غير محدد في طلب ترحيل الباقة.");
            }

            var migrationCheck = await _offerSubscriptionEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!migrationCheck.Allowed)
            {
                return Fail(entity, migrationCheck.MessageAr);
            }

            entity.PriorProductId ??= migrationCheck.PriorProductId;
            entity.PriorProductOfferingId ??= migrationCheck.PriorProductOfferingId;
        }

        if (entity.Kind == TelecomOperationKind.TemporarySuspension)
        {
            if (string.IsNullOrEmpty(entity.MsisdnAssetId))
            {
                return Fail(entity, "رقم الخط غير محدد في طلب الحظر.");
            }

            var suspensionCheck = await _suspensionEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!suspensionCheck.Allowed)
            {
                return Fail(entity, suspensionCheck.MessageAr);
            }
        }

        if (entity.Kind == TelecomOperationKind.Reconnect)
        {
            if (string.IsNullOrEmpty(entity.MsisdnAssetId))
            {
                return Fail(entity, "رقم الخط غير محدد في طلب إعادة التفعيل.");
            }

            if (string.Equals(entity.ApprovalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
                && !entity.FraudClearanceConfirmed
                && !string.IsNullOrWhiteSpace(actorUserId))
            {
                entity.FraudClearanceConfirmed = true;
                entity.FraudClearanceByUserId = actorUserId;
                _operationRepository.Update(entity);
            }

            var reconnectCheck = await _reconnectEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!reconnectCheck.Allowed)
            {
                return Fail(entity, reconnectCheck.MessageAr);
            }

            entity.SourceSuspensionOperationId ??= reconnectCheck.SourceSuspensionOperationId;
        }

        if (entity.Kind == TelecomOperationKind.DeviceSale)
        {
            var deviceCheck = await _deviceSalesEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!deviceCheck.Allowed)
            {
                return Fail(entity, deviceCheck.MessageAr);
            }
        }

        if (entity.Kind == TelecomOperationKind.DepositRefundSettlement)
        {
            var refundCheck = await _refundEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!refundCheck.Allowed)
            {
                return Fail(entity, refundCheck.MessageAr);
            }
        }

        if (entity.Kind == TelecomOperationKind.BadDebtRecovery)
        {
            if (string.IsNullOrEmpty(entity.MsisdnAssetId))
            {
                return Fail(entity, "رقم الخط غير محدد في طلب التحصيل.");
            }

            if (string.Equals(entity.ApprovalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
                && !entity.FraudClearanceConfirmed
                && !string.IsNullOrWhiteSpace(actorUserId))
            {
                entity.FraudClearanceConfirmed = true;
                entity.FraudClearanceByUserId = actorUserId;
                _operationRepository.Update(entity);
            }

            var badDebtCheck = await _badDebtEligibility.ValidateForConfirmAsync(entity, cancellationToken);
            if (!badDebtCheck.Allowed)
            {
                return Fail(entity, badDebtCheck.MessageAr);
            }
        }

        BindSubscriptionResult? bindResult = null;
        var deviceSaleFulfilled = false;
        var refundFulfilled = false;
        var badDebtFulfilled = false;
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
                else if (entity.Kind == TelecomOperationKind.ChangeGsmType)
                {
                    await ApplyChangeGsmTypeAsync(entity, actorUserId, ct);
                    provisionCtx = await BuildProvisionContextAsync(entity, ct);
                }
                else if (entity.Kind == TelecomOperationKind.NumberPortability
                         && !string.IsNullOrEmpty(entity.TargetMsisdnAssetId))
                {
                    provisionCtx = await ApplyChangeNumberAsync(entity, actorUserId, ct);
                }
                else if (entity.Kind == TelecomOperationKind.Termination
                         && !string.IsNullOrEmpty(entity.MsisdnAssetId))
                {
                    provisionCtx = await ApplyTerminationAsync(entity, actorUserId, ct);
                }
                else if (entity.Kind == TelecomOperationKind.TemporarySuspension
                         && !string.IsNullOrEmpty(entity.MsisdnAssetId))
                {
                    provisionCtx = await ApplySuspensionAsync(entity, actorUserId, ct);
                }
                else if (entity.Kind == TelecomOperationKind.Reconnect
                         && !string.IsNullOrEmpty(entity.MsisdnAssetId))
                {
                    provisionCtx = await ApplyReconnectAsync(entity, actorUserId, ct);
                }
                else if (entity.Kind == TelecomOperationKind.DeviceSale)
                {
                    await _deviceSaleCompletion.FulfillAsync(entity, actorUserId, ct);
                    deviceSaleFulfilled = true;
                }
                else if (entity.Kind == TelecomOperationKind.DepositRefundSettlement)
                {
                    await _refundCompletion.FulfillAsync(entity, actorUserId, ct);
                    refundFulfilled = true;
                }
                else if (entity.Kind == TelecomOperationKind.BadDebtRecovery)
                {
                    await ApplyBadDebtRecoveryAsync(entity, actorUserId, ct);
                    await _badDebtCompletion.FulfillAsync(entity, actorUserId, ct);
                    badDebtFulfilled = true;
                }

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
            return Fail(entity, ex.Message);
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

            if (bindResult != null)
            {
                await _compensator.CompensateAsync(
                    bindResult.TelecomSubscription.Id,
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
                    billingResult.Message,
                    cancellationToken);
            }

            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, billingResult.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            var falloutSource = _billingRouting.IntegrationFalloutSource(entity.Kind, lineContext.SubscriptionTypeCode);
            await _ticketQueue.EnqueueProvisioningFalloutAsync(
                entity, $"{falloutSource}: {billingResult.Message}", actorUserId, cancellationToken);
            return Fail(entity, billingResult.Message);
        }

        if (entity.Kind == TelecomOperationKind.Termination)
        {
            entity.FinalBillAmount ??= 12_500m;
            entity.DepositSettlementAmount ??= 0m;
            entity.DepositSettlementStatus ??= "Settled";
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
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
        if (entity.Kind is TelecomOperationKind.Termination
            or TelecomOperationKind.TemporarySuspension
            or TelecomOperationKind.Reconnect
            or TelecomOperationKind.DeviceSale
            or TelecomOperationKind.DepositRefundSettlement
            or TelecomOperationKind.BadDebtRecovery)
        {
            return;
        }

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
            var reservationDuration = await _inventoryRules.GetMsisdnReservationDurationAsync(cancellationToken);
            asset.ReserveForCustomer(customerId, utcNow, reservationDuration);
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

    private const string LostStolenPreSwapSuspensionReason =
        "Automated pre-swap lockdown for lost/stolen asset recovery";

    private async Task EnsureLostStolenPreSwapSuspensionAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (!entity.IsLostOrStolenReport)
        {
            return;
        }

        var profile = await _profileRepository.GetAsync(entity.SubscriberProfileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        MsisdnAsset? msisdnAsset = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            msisdnAsset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
        }

        var alreadySuspended = profile.OperationalStatus is SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound
            || msisdnAsset?.PoolStatus == MsisdnPoolStatus.Suspended;

        if (alreadySuspended)
        {
            return;
        }

        entity.SuspensionType ??= SuspensionWellKnown.Operational;
        entity.SuspensionReason ??= LostStolenPreSwapSuspensionReason;
        entity.BarringLevel ??= SuspensionWellKnown.BarringFull;
        entity.PriorOperationalStatus ??= profile.OperationalStatus.ToString();
        entity.BarStatus ??= "Pending";
        entity.SuspensionStartDateUtc ??= DateTime.UtcNow;

        profile.Suspend(msisdnAsset?.Msisdn ?? "");
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        if (msisdnAsset != null && msisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            msisdnAsset.TransitionTo(MsisdnPoolStatus.Suspended);
            msisdnAsset.UpdatedById = actorUserId;
            _msisdnRepository.Update(msisdnAsset);
        }
    }

    private async Task<OperationProvisionContext> ApplySimSwapAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsureLostStolenPreSwapSuspensionAsync(entity, actorUserId, cancellationToken);

        var newSim = await _simRepository.GetAsync(entity.SimInventoryId!, cancellationToken)
            ?? throw new BusinessRuleViolationException("الشريحة الجديدة غير موجودة.");

        if (newSim.Status is not (SimStatus.Available or SimStatus.Reserved))
        {
            throw new BusinessRuleViolationException("الشريحة الجديدة غير متاحة للتبديل.");
        }

        entity.PriorSimInventoryId ??= await ResolvePriorSimIdForSwapAsync(entity, cancellationToken);

        if (!string.IsNullOrEmpty(entity.PriorSimInventoryId))
        {
            var priorSim = await _simRepository.GetAsync(entity.PriorSimInventoryId, cancellationToken);
            if (priorSim != null && priorSim.Status == SimStatus.Active)
            {
                if (entity.IsLostOrStolenReport)
                {
                    priorSim.MarkBurned(DateTime.UtcNow);
                }
                else
                {
                    priorSim.TransitionTo(SimStatus.Quarantined);
                }

                priorSim.UpdatedById = actorUserId;
                _simRepository.Update(priorSim);
            }
        }

        var oldSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == entity.SubscriberProfileId
                && s.Status == SimStatus.Active
                && s.Id != entity.SimInventoryId
                && s.Id != entity.PriorSimInventoryId)
            .ToListAsync(cancellationToken);

        foreach (var old in oldSims)
        {
            old.TransitionTo(SimStatus.Quarantined);
            old.UpdatedById = actorUserId;
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

        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var msisdnAsset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
            if (msisdnAsset != null)
            {
                msisdnAsset.PairedIccid = newSim.Iccid;
                msisdnAsset.PairedImsi = newSim.Imsi;
                if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Suspended)
                {
                    msisdnAsset.TransitionTo(MsisdnPoolStatus.Active);
                }

                msisdnAsset.UpdatedById = actorUserId;
                _msisdnRepository.Update(msisdnAsset);
            }
        }

        return await BuildProvisionContextAsync(entity, cancellationToken, newSim.Iccid);
    }

    private async Task<string?> ResolvePriorSimIdForSwapAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(entity.PriorSimInventoryId))
        {
            return entity.PriorSimInventoryId;
        }

        return await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                && s.SubscriberProfileId == entity.SubscriberProfileId
                && s.Status == SimStatus.Active
                && s.Id != entity.SimInventoryId)
            .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
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
        await _provisionedDispatcher.DispatchAsync(
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

    private async Task<OperationProvisionContext> ApplyChangeNumberAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
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

        return await BuildProvisionContextForMsisdnAsync(
            entity,
            targetAsset.Msisdn,
            priorAsset.Msisdn,
            cancellationToken);
    }

    private async Task<OperationProvisionContext> BuildProvisionContextForMsisdnAsync(
        TelecomOperationRequest entity,
        string? msisdn,
        string? priorMsisdn,
        CancellationToken cancellationToken)
    {
        string? iccid = null;
        if (!string.IsNullOrEmpty(entity.SimInventoryId))
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
            entity.TargetMsisdnAssetId,
            entity.SimInventoryId);
    }

    private static bool RequiresNetworkProvision(TelecomOperationKind kind) =>
        kind is TelecomOperationKind.NewActivation
            or TelecomOperationKind.SimSwap
            or TelecomOperationKind.Migration
            or TelecomOperationKind.TakeOver
            or TelecomOperationKind.ChangeGsmType
            or TelecomOperationKind.NumberPortability
            or TelecomOperationKind.Termination
            or TelecomOperationKind.TemporarySuspension
            or TelecomOperationKind.Reconnect;

    private async Task<OperationProvisionContext> ApplySuspensionAsync(
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
        ApplyBarringToProfile(profile, entity.BarringLevel ?? SuspensionWellKnown.BarringFull, msisdn);
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

        return await BuildProvisionContextAsync(entity, cancellationToken);
    }

    private async Task<OperationProvisionContext> ApplyReconnectAsync(
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

        return await BuildProvisionContextAsync(entity, cancellationToken);
    }

    private async Task ApplyBadDebtRecoveryAsync(
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

        // GLOBAL HARDENING: BDR Settled Stage
        if (string.Equals(entity.CollectionAction, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entity.CollectionAction, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase))
        {
            entity.CollectionSettlementStatus = BadDebtWellKnown.SettlementCompleted;
            entity.DunningStage = BadDebtWellKnown.Settled;
        }

        entity.ProvisioningResult = "Pending";
    }

    private static void ApplyBarringToProfile(SubscriberProfile profile, string barringLevel, string msisdn)
    {
        if (string.Equals(barringLevel, SuspensionWellKnown.BarringInboundOnly, StringComparison.OrdinalIgnoreCase))
        {
            profile.SuspendInbound(msisdn);
        }
        else if (string.Equals(barringLevel, SuspensionWellKnown.BarringOutboundOnly, StringComparison.OrdinalIgnoreCase))
        {
            profile.SuspendOutbound(msisdn);
        }
        else if (string.Equals(barringLevel, SuspensionWellKnown.BarringDataOnly, StringComparison.OrdinalIgnoreCase))
        {
            // Voice/SMS remain active; data context is barred at HLR/CBS.
        }
        else
        {
            profile.Suspend(msisdn);
        }
    }

    private async Task<OperationProvisionContext> ApplyTerminationAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var msisdnAssetId = entity.MsisdnAssetId
            ?? throw new BusinessRuleViolationException("رقم الخط غير محدد.");

        var profile = await _profileRepository.GetAsync(entity.SubscriberProfileId, cancellationToken)
            ?? throw new BusinessRuleViolationException("ملف المشترك غير موجود.");

        if (profile.OperationalStatus == SubscriberOperationalStatus.Terminated)
        {
            throw new BusinessRuleViolationException("VAL-10-04: الخط منتهٍ مسبقاً.");
        }

        profile.Terminate();
        profile.UpdatedById = actorUserId;
        _profileRepository.Update(profile);

        var msisdnAsset = await _msisdnRepository.GetAsync(msisdnAssetId, cancellationToken)
            ?? throw new BusinessRuleViolationException("أصل الرقم غير موجود.");

        if (msisdnAsset.PoolStatus == MsisdnPoolStatus.Active)
        {
            msisdnAsset.TransitionTo(MsisdnPoolStatus.Quarantined);
        }

        msisdnAsset.SubscriberProfileId = null;
        msisdnAsset.ReservedForCustomerId = null;
        msisdnAsset.ReservedUntilUtc = null;
        msisdnAsset.UpdatedById = actorUserId;
        _msisdnRepository.Update(msisdnAsset);

        var activeSims = await _simRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == entity.SubscriberProfileId
                        && s.Status == SimStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var sim in activeSims)
        {
            sim.TransitionTo(SimStatus.Quarantined);
            sim.AssignToProfile(null);
            sim.UpdatedById = actorUserId;
            _simRepository.Update(sim);
        }

        entity.PriorSimInventoryId ??= activeSims
            .OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc)
            .Select(s => s.Id)
            .FirstOrDefault();

        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted
                        && s.SubscriberProfileId == entity.SubscriberProfileId
                        && s.MsisdnAssetId == msisdnAssetId)
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            sub.IsDeleted = true;
            sub.UpdatedById = actorUserId;
            _subscriptionRepository.Update(sub);
        }

        entity.DeprovisionStatus = "Pending";
        entity.PriorMsisdnAssetId ??= msisdnAssetId;

        return await BuildProvisionContextAsync(entity, cancellationToken);
    }

    private async Task<OperationProvisionContext> ApplyTakeOverAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        entity.PriorSubscriberProfileId ??= entity.SubscriberProfileId;

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
            return;
        }

        entity.PriorProductId ??= subscription.ProductId;
        entity.PriorProductOfferingId ??= subscription.ProductOfferingId;

        subscription.ProductId = entity.ProductId;
        subscription.ProductOfferingId = entity.ProductOfferingId;
        subscription.UpdatedById = actorUserId;
        _subscriptionRepository.Update(subscription);
    }

    private async Task ApplyChangeGsmTypeAsync(
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

    private static bool HasKycProofForActivation(TelecomOperationRequest entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.KycDocumentReferenceId))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(entity.OverrideReasonCode))
        {
            return true;
        }

        if (entity.KycVerifiedAtUtc.HasValue)
        {
            return true;
        }

        if (entity.DocumentStatus is TelecomDocumentStatus.Uploaded or TelecomDocumentStatus.Verified)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(entity.IdentityDocumentStorageKey))
        {
            return true;
        }

        return false;
    }

    private static TelecomActivationWorkflowResult Fail(TelecomOperationRequest entity, string message) =>
        Fail(entity, new BilingualUserMessage(message, message));

    private static TelecomActivationWorkflowResult Fail(TelecomOperationRequest entity, BilingualUserMessage message) =>
        new(
            entity,
            new BillingProvisionResult(false, message.ResolveForCurrentCulture()),
            null,
            false,
            message.ResolveForCurrentCulture(),
            message.Ar,
            message.En);
}
