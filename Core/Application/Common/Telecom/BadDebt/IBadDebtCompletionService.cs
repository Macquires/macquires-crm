using Domain.Entities;

namespace Application.Common.Telecom.BadDebt;

public interface IBadDebtCompletionService
{
    Task FulfillAsync(TelecomOperationRequest operation, string? actorUserId, CancellationToken cancellationToken = default);

    Task CompensateOnFailureAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}
