using Application.Common.Security;

namespace Application.Common.Telecom.Analytics;

public interface IOperationalAnalyticsScopeService
{
    Task<StrategicDataScope> ResolveScopeAsync(
        string? requestedRegionId,
        string? requestedBranchId,
        CancellationToken cancellationToken = default);
}
