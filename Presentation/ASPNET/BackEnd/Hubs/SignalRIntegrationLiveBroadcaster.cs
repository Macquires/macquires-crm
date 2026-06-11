using Application.Common.Integrations;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

public sealed class SignalRIntegrationLiveBroadcaster : IIntegrationLiveBroadcaster
{
    private readonly IHubContext<IntegrationLiveHub> _hub;

    public SignalRIntegrationLiveBroadcaster(IHubContext<IntegrationLiveHub> hub) => _hub = hub;

    public Task BroadcastAsync(
        TelecomIntegrationSystem system,
        string operation,
        string? msisdn,
        string message,
        bool success,
        CancellationToken cancellationToken = default)
    {
        return _hub.Clients.Group("integration-console").SendAsync(
            "integrationEvent",
            new
            {
                atUtc = DateTime.UtcNow,
                system = system.ToString(),
                operation,
                msisdn,
                message,
                success
            },
            cancellationToken);
    }
}
