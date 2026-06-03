using Domain.Entities;

namespace Application.Common.Telecom.Refund;

public interface IRefundCompletionService
{
    Task FulfillAsync(TelecomOperationRequest operation, string? actorUserId, CancellationToken cancellationToken = default);

    Task CompensateOnFailureAsync(TelecomOperationRequest operation, CancellationToken cancellationToken = default);
}
