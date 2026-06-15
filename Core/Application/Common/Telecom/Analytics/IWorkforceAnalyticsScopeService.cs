using Application.Common.Security;

namespace Application.Common.Telecom.Analytics;

public sealed class WorkforceAnalyticsScope
{
    public StrategicDataScope DataScope { get; init; } = null!;
    public IReadOnlyList<string> AllowedUserIds { get; init; } = [];
    public IReadOnlyList<WorkforceUserSnapshot> Users { get; init; } = [];
}

public interface IWorkforceAnalyticsScopeService
{
    Task<WorkforceAnalyticsScope> ResolveAsync(
        string? requestedRegionId,
        string? requestedBranchId,
        CancellationToken cancellationToken = default);

    Task<bool> CanViewUserAsync(string targetUserId, CancellationToken cancellationToken = default);
}
