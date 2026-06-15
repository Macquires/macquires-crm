using Application.Common.Integrations;

namespace Application.Common.Telecom.Analytics;

public interface IExecutiveAlertBroadcaster
{
    Task BroadcastCriticalCountAsync(
        int criticalCount,
        int warningCount,
        string scopeLabelAr,
        CancellationToken cancellationToken = default);
}
