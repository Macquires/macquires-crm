using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom;

public sealed class TelecomOperationOrchestrator : ITelecomOperationOrchestrator
{
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;

    public TelecomOperationOrchestrator(ICommandRepository<TelecomOperationAuditLog> auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task TransitionAsync(
        TelecomOperationRequest operation,
        TelecomOperationStatus toStatus,
        string? actorUserId,
        string? note,
        CancellationToken cancellationToken)
    {
        var from = operation.Status;
        if (from == toStatus) return;

        TelecomOperationLifecycle.EnsureCanTransition(from, toStatus);

        var audit = new TelecomOperationAuditLog
        {
            TelecomOperationRequestId = operation.Id,
            FromStatus = from,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            Note = note,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedById = actorUserId
        };

        operation.Status = toStatus;
        operation.UpdatedById = actorUserId;
        operation.UpdatedAtUtc = DateTime.UtcNow;

        if (toStatus == TelecomOperationStatus.Confirmed)
        {
            operation.ConfirmedAtUtc ??= DateTime.UtcNow;
        }

        await _auditRepository.CreateAsync(audit, cancellationToken);
    }
}
