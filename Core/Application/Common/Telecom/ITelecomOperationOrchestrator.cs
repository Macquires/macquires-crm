using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom;

public interface ITelecomOperationOrchestrator
{
    Task TransitionAsync(
        TelecomOperationRequest operation,
        TelecomOperationStatus toStatus,
        string? actorUserId,
        string? note,
        CancellationToken cancellationToken);
}
