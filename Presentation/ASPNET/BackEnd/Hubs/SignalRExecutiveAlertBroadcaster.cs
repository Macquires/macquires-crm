using Application.Common.Telecom.Analytics;
using Microsoft.AspNetCore.SignalR;

namespace ASPNET.BackEnd.Hubs;

public sealed class SignalRExecutiveAlertBroadcaster : IExecutiveAlertBroadcaster
{
    private readonly IHubContext<ExecutiveAlertsHub> _hub;

    public SignalRExecutiveAlertBroadcaster(IHubContext<ExecutiveAlertsHub> hub) => _hub = hub;

    public Task BroadcastCriticalCountAsync(
        int criticalCount,
        int warningCount,
        string scopeLabelAr,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.Group("executive-mis").SendAsync(
            "executiveAlert",
            new
            {
                atUtc = DateTime.UtcNow,
                criticalCount,
                warningCount,
                scopeLabelAr,
            },
            cancellationToken);
}
