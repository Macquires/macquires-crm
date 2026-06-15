using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public class GetTelecomDashboardKpisResult
{
    public decimal Arpu { get; init; }
    public decimal ChurnPercent30 { get; init; }
    public decimal ChurnPercent60 { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal RevenueChangePercent { get; init; }
    public List<GetTelecomBranchHeatDto> BranchHeat { get; init; } = new();

    /// <summary>Backward-compatible alias for legacy MIS clients.</summary>
    public decimal ArpuDemo => Arpu;

    /// <summary>Backward-compatible alias — maps to 30-day churn window.</summary>
    public decimal ChurnPercentDemo => ChurnPercent30;

    public string AccessLevel { get; init; } = null!;
    public string ScopeLabelAr { get; init; } = null!;
    public bool CanUseFilters { get; init; }
    public string? EffectiveRegionId { get; init; }
    public string? EffectiveBranchId { get; init; }
    public IReadOnlyList<StrategicScopeOptionDto> Regions { get; init; } = [];
    public IReadOnlyList<StrategicScopeOptionDto> Branches { get; init; } = [];
}

public record GetTelecomBranchHeatDto(
    string BranchId,
    string BranchName,
    decimal Revenue,
    int ActiveSubscriptions)
{
  /// <summary>Legacy field — revenue from live payment ledger.</summary>
    public decimal RevenueDemo => Revenue;
}

public record GetTelecomDashboardKpisRequest(string? RegionId, string? BranchId)
    : IRequest<GetTelecomDashboardKpisResult>, IOperationalKpiRequest;

public class GetTelecomDashboardKpisHandler : IRequestHandler<GetTelecomDashboardKpisRequest, GetTelecomDashboardKpisResult>
{
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IExecutiveFinancialMetricsService _financialMetrics;
    private readonly IOperatorContext _operator;
    private readonly IUserAuditService _audit;

    public GetTelecomDashboardKpisHandler(
        IOperationalAnalyticsScopeService scopeService,
        IExecutiveFinancialMetricsService financialMetrics,
        IOperatorContext operatorContext,
        IUserAuditService audit)
    {
        _scopeService = scopeService;
        _financialMetrics = financialMetrics;
        _operator = operatorContext;
        _audit = audit;
    }

    public async Task<GetTelecomDashboardKpisResult> Handle(
        GetTelecomDashboardKpisRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _operator.UserId
            ?? throw new UnauthorizedAccessException("يجب تسجيل الدخول لعرض تقارير MIS.");

        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (scope.EffectiveBranchIds.Count == 0)
        {
            return Empty(scope);
        }

        var metrics = await _financialMetrics.ComputeAsync(
            scope.EffectiveBranchIds,
            DateTime.UtcNow,
            billingCycleDays: 30,
            cancellationToken);

        await _audit.LogAsync(new UserAuditLogRequest
        {
            ActorUserId = userId,
            ActionType = UserAuditActionTypes.StrategicReportViewed,
            SummaryAr = $"عرض تقارير MIS — {scope.ScopeLabelAr}",
            Payload = new
            {
                report = "mis-dashboard-kpis",
                scopeLabelAr = scope.ScopeLabelAr,
                accessLevel = scope.AccessLevel.ToString(),
            },
        }, cancellationToken);

        return new GetTelecomDashboardKpisResult
        {
            Arpu = metrics.Arpu,
            ChurnPercent30 = metrics.ChurnPercent30,
            ChurnPercent60 = metrics.ChurnPercent60,
            TotalRevenue = metrics.TotalRevenue,
            RevenueChangePercent = metrics.RevenueChangePercent,
            BranchHeat = metrics.BranchHeat
                .Select(b => new GetTelecomBranchHeatDto(b.BranchId, b.BranchName, b.Revenue, b.ActiveSubscriptions))
                .ToList(),
            AccessLevel = scope.AccessLevel.ToString(),
            ScopeLabelAr = scope.ScopeLabelAr,
            CanUseFilters = scope.CanUseFilters,
            EffectiveRegionId = scope.EffectiveRegionId,
            EffectiveBranchId = scope.EffectiveBranchId,
            Regions = scope.Regions,
            Branches = scope.Branches,
        };
    }

    private static GetTelecomDashboardKpisResult Empty(StrategicDataScope scope) =>
        new()
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
