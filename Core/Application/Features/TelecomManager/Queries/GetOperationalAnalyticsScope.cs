using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public class GetOperationalAnalyticsScopeResult
{
    public string AccessLevel { get; init; } = null!;
    public string ScopeLabelAr { get; init; } = null!;
    public bool CanUseFilters { get; init; }
    public string? EffectiveRegionId { get; init; }
    public string? EffectiveBranchId { get; init; }
    public IReadOnlyList<StrategicScopeOptionDto> Regions { get; init; } = [];
    public IReadOnlyList<StrategicScopeOptionDto> Branches { get; init; } = [];
}

public record GetOperationalAnalyticsScopeRequest(string? RegionId, string? BranchId)
    : IRequest<GetOperationalAnalyticsScopeResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => BackOfficeDashboardPermissionSets.AccessAny;
}

public class GetOperationalAnalyticsScopeHandler
    : IRequestHandler<GetOperationalAnalyticsScopeRequest, GetOperationalAnalyticsScopeResult>
{
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IOperatorContext _operator;

    public GetOperationalAnalyticsScopeHandler(
        IOperationalAnalyticsScopeService scopeService,
        IOperatorContext operatorContext)
    {
        _scopeService = scopeService;
        _operator = operatorContext;
    }

    public async Task<GetOperationalAnalyticsScopeResult> Handle(
        GetOperationalAnalyticsScopeRequest request,
        CancellationToken cancellationToken)
    {
        if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
        {
            throw new UnauthorizedAccessException("يجب تسجيل الدخول لعرض نطاق العمليات.");
        }

        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        return new GetOperationalAnalyticsScopeResult
        {
            AccessLevel = scope.AccessLevel.ToString(),
            ScopeLabelAr = scope.ScopeLabelAr,
            CanUseFilters = scope.CanUseFilters,
            EffectiveRegionId = scope.EffectiveRegionId,
            EffectiveBranchId = scope.EffectiveBranchId,
            Regions = scope.Regions,
            Branches = scope.Branches,
        };
    }
}
