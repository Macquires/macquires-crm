using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telecom.Analytics;

public sealed class OperationalAnalyticsScopeService : IOperationalAnalyticsScopeService
{
    private readonly IStrategicDataScopeService _scopeService;
    private readonly IOperatorContext _operator;

    public OperationalAnalyticsScopeService(
        IStrategicDataScopeService scopeService,
        IOperatorContext operatorContext)
    {
        _scopeService = scopeService;
        _operator = operatorContext;
    }

    public async Task<StrategicDataScope> ResolveScopeAsync(
        string? requestedRegionId,
        string? requestedBranchId,
        CancellationToken cancellationToken = default)
    {
        var userId = _operator.UserId
            ?? throw new UnauthorizedAccessException("يجب تسجيل الدخول لعرض مؤشرات العمليات.");

        var scope = await _scopeService.ResolveScopeAsync(
            userId,
            requestedRegionId,
            requestedBranchId,
            cancellationToken);

        if (!scope.CanUseFilters && (requestedRegionId != null || requestedBranchId != null))
        {
            scope = await _scopeService.ResolveScopeAsync(userId, null, null, cancellationToken);
        }

        return scope;
    }
}
