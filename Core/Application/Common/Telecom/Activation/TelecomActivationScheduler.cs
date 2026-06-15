using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.Activation;

public interface ITelecomActivationScheduler
{
    Task<TelecomActivationWorkflowResult> DeferToScheduledAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken);
}

public sealed class TelecomActivationScheduler : ITelecomActivationScheduler
{
    private readonly ITelecomOperationOrchestrator _orchestrator;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TelecomActivationScheduler(
        ITelecomOperationOrchestrator orchestrator,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork)
    {
        _orchestrator = orchestrator;
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TelecomActivationWorkflowResult> DeferToScheduledAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
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
                    entity, TelecomOperationStatus.Scheduled, actorUserId, "جدولة حتى تاريخ السريان", ct);

                entity.ConfirmedAtUtc ??= DateTime.UtcNow;
                _operationRepository.Update(entity);
                await _unitOfWork.SaveAsync(ct);
            }, cancellationToken);
        }
        catch (BusinessRuleViolationException ex)
        {
            await _orchestrator.TransitionAsync(entity, TelecomOperationStatus.Failed, actorUserId, ex.Message, cancellationToken);
            _operationRepository.Update(entity);
            await _unitOfWork.SaveAsync(cancellationToken);
            return TelecomActivationWorkflowSupport.Fail(entity, ex.Message);
        }

        var msgAr = TelecomActivationWorkflowSupport.BuildScheduledMessageAr(entity);
        var msgEn = TelecomActivationWorkflowSupport.BuildScheduledMessageEn(entity);
        return new TelecomActivationWorkflowResult(
            entity,
            new BillingProvisionResult(true, msgAr),
            null,
            false,
            msgAr,
            msgAr,
            msgEn);
    }
}
