using Application.Common.Security;
using Application.Common.Telecom.Analytics;

namespace Infrastructure.Telecom.Analytics;

public sealed class WorkforceAnalyticsScopeService : IWorkforceAnalyticsScopeService
{
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IWorkforceUserReadService _userRead;
    private readonly IOperatorContext _operator;

    public WorkforceAnalyticsScopeService(
        IOperationalAnalyticsScopeService scopeService,
        IWorkforceUserReadService userRead,
        IOperatorContext operatorContext)
    {
        _scopeService = scopeService;
        _userRead = userRead;
        _operator = operatorContext;
    }

    public async Task<WorkforceAnalyticsScope> ResolveAsync(
        string? requestedRegionId,
        string? requestedBranchId,
        CancellationToken cancellationToken = default)
    {
        var dataScope = await _scopeService.ResolveScopeAsync(
            requestedRegionId,
            requestedBranchId,
            cancellationToken);

        if (dataScope.EffectiveBranchIds.Count == 0)
        {
            return new WorkforceAnalyticsScope { DataScope = dataScope };
        }

        var users = await _userRead.GetUsersInBranchesAsync(
            dataScope.EffectiveBranchIds,
            cancellationToken);

        return new WorkforceAnalyticsScope
        {
            DataScope = dataScope,
            Users = users,
            AllowedUserIds = users.Select(u => u.UserId).ToList(),
        };
    }

    public async Task<bool> CanViewUserAsync(string targetUserId, CancellationToken cancellationToken = default)
    {
        var scope = await ResolveAsync(null, null, cancellationToken);
        return scope.AllowedUserIds.Contains(targetUserId, StringComparer.Ordinal);
    }
}
