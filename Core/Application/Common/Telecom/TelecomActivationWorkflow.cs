using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom.Activation;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.OperationConfirm;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom;

/// <summary>
/// Enterprise activation: DB transaction (reserve + bind) → CBS → domain notification → HLR/SMS handlers.
/// </summary>
public sealed class TelecomActivationWorkflow : ITelecomActivationWorkflow
{
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IOperationConfirmStrategyRegistry _confirmStrategies;
    private readonly IBackOfficePaymentReferenceValidator _paymentValidator;
    private readonly ITelecomActivationScheduler _scheduler;
    private readonly ITelecomActivationProvisioningExecutor _provisioningExecutor;
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly IUnitOfWork _unitOfWork;

    public TelecomActivationWorkflow(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IOperationConfirmStrategyRegistry confirmStrategies,
        IBackOfficePaymentReferenceValidator paymentValidator,
        ITelecomActivationScheduler scheduler,
        ITelecomActivationProvisioningExecutor provisioningExecutor,
        ITelecomOperationOrchestrator orchestrator,
        IUnitOfWork unitOfWork)
    {
        _operationRepository = operationRepository;
        _confirmStrategies = confirmStrategies;
        _paymentValidator = paymentValidator;
        _scheduler = scheduler;
        _provisioningExecutor = provisioningExecutor;
        _orchestrator = orchestrator;
        _unitOfWork = unitOfWork;
    }

    public async Task<TelecomActivationWorkflowResult> ConfirmActivationAsync(
        string operationId,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await _operationRepository.GetAsync(operationId, cancellationToken);
        if (entity == null)
        {
            return TelecomActivationWorkflowSupport.Fail(new TelecomOperationRequest(), "Operation not found.");
        }

        entity.CorrelationId ??= Guid.CreateVersion7().ToString();

        if (entity.Status == TelecomOperationStatus.Completed)
        {
            return new TelecomActivationWorkflowResult(
                entity,
                new BillingProvisionResult(true, TelecomActivationWorkflowSupport.IdempotentMessageAr),
                null,
                true,
                TelecomActivationWorkflowSupport.IdempotentMessageAr);
        }

        if (entity.Status == TelecomOperationStatus.Scheduled)
        {
            var scheduledAr = TelecomActivationWorkflowSupport.BuildScheduledMessageAr(entity);
            return new TelecomActivationWorkflowResult(
                entity,
                new BillingProvisionResult(true, scheduledAr),
                null,
                true,
                scheduledAr,
                scheduledAr,
                TelecomActivationWorkflowSupport.BuildScheduledMessageEn(entity));
        }

        if (entity.DocumentStatus < TelecomDocumentStatus.Uploaded)
        {
            return TelecomActivationWorkflowSupport.Fail(entity, "يجب رفع الوثائق قبل التأكيد.");
        }

        var allowed = new[]
        {
            TelecomOperationStatus.Draft,
            TelecomOperationStatus.PendingDocuments,
            TelecomOperationStatus.PendingExternal
        };

        if (!allowed.Contains(entity.Status))
        {
            return TelecomActivationWorkflowSupport.Fail(entity, "حالة الطلب لا تسمح بالتأكيد.");
        }

        var confirmStrategy = RequireConfirmStrategy(entity.Kind);
        var validation = await confirmStrategy.ValidateForConfirmAsync(entity, actorUserId, cancellationToken);
        if (!validation.Allowed)
        {
            return TelecomActivationWorkflowSupport.Fail(entity, validation.MessageAr);
        }

        var paymentValidation = await _paymentValidator.ValidateReconnectPaymentAsync(entity, cancellationToken);
        if (!paymentValidation.IsValid)
        {
            return TelecomActivationWorkflowSupport.Fail(entity, paymentValidation.MessageAr);
        }

        if (entity.FraudClearanceConfirmed)
        {
            _operationRepository.Update(entity);
        }

        if (TelecomOperationSchedulePolicy.ShouldDeferToScheduled(entity, DateTime.UtcNow))
        {
            return await _scheduler.DeferToScheduledAsync(entity, actorUserId, cancellationToken);
        }

        return await _provisioningExecutor.ExecuteAsync(entity, confirmStrategy, actorUserId, cancellationToken);
    }

    public async Task<TelecomActivationWorkflowResult> ExecuteScheduledOperationAsync(
        string operationId,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await _operationRepository.GetAsync(operationId, cancellationToken);
        if (entity == null)
        {
            return TelecomActivationWorkflowSupport.Fail(new TelecomOperationRequest(), "Operation not found.");
        }

        if (entity.Status == TelecomOperationStatus.Completed)
        {
            return new TelecomActivationWorkflowResult(
                entity,
                new BillingProvisionResult(true, TelecomActivationWorkflowSupport.IdempotentMessageAr),
                null,
                true,
                TelecomActivationWorkflowSupport.IdempotentMessageAr);
        }

        if (entity.Status != TelecomOperationStatus.Scheduled)
        {
            return TelecomActivationWorkflowSupport.Fail(entity, "العملية ليست في حالة مجدولة.");
        }

        if (!TelecomOperationSchedulePolicy.IsDueForExecution(entity, DateTime.UtcNow))
        {
            return TelecomActivationWorkflowSupport.Fail(entity, "لم يحن موعد التنفيذ بعد.");
        }

        var confirmStrategy = RequireConfirmStrategy(entity.Kind);
        var validation = await confirmStrategy.ValidateForConfirmAsync(entity, actorUserId, cancellationToken);
        if (!validation.Allowed)
        {
            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, validation.MessageAr, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            return TelecomActivationWorkflowSupport.Fail(entity, validation.MessageAr);
        }

        return await _provisioningExecutor.ExecuteAsync(entity, confirmStrategy, actorUserId, cancellationToken);
    }

    private IOperationConfirmStrategy RequireConfirmStrategy(TelecomOperationKind kind) =>
        _confirmStrategies.Resolve(kind)
        ?? throw new InvalidOperationException($"No confirm strategy registered for {kind}.");
}
