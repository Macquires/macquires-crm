using Domain.Entities;

namespace Application.Common.Telecom.DeviceSales;

public interface IDeviceSaleCompletionService
{
    Task FulfillAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default);

    Task CompensateOnFailureAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}
