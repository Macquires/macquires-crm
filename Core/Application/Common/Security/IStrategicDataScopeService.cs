namespace Application.Common.Security;

public interface IStrategicDataScopeService
{
    Task<StrategicDataScope> ResolveScopeAsync(
        string userId,
        string? requestedRegionId,
        string? requestedBranchId,
        CancellationToken cancellationToken = default);
}
