using Domain.Enums;

namespace Application.Common.Integrations;

public interface IIntegrationLiveBroadcaster
{
    Task BroadcastAsync(
        TelecomIntegrationSystem system,
        string operation,
        string? msisdn,
        string message,
        bool success,
        CancellationToken cancellationToken = default);
}
