using Application.Common.Events;

namespace Application.Common.Telecom;

public interface ITelecomProvisionedEventDispatcher
{
    Task DispatchAsync(TelecomOperationProvisionedNotification notification, CancellationToken cancellationToken = default);
}
