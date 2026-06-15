using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public sealed class BranchGeoHeatmapPointDto
{
    public string BranchId { get; init; } = null!;
    public string BranchName { get; init; } = null!;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public decimal Revenue { get; init; }
    public int ActiveSubscriptions { get; init; }
    /// <summary>0–100 heat intensity derived from revenue share.</summary>
    public int HeatScore { get; init; }
}

public class GetBranchGeoHeatmapResult
{
    public string ScopeLabelAr { get; init; } = null!;
    public IReadOnlyList<BranchGeoHeatmapPointDto> Points { get; init; } = [];
}

public record GetBranchGeoHeatmapRequest(string? RegionId, string? BranchId)
    : IRequest<GetBranchGeoHeatmapResult>, IOperationalKpiRequest;

public class GetBranchGeoHeatmapHandler : IRequestHandler<GetBranchGeoHeatmapRequest, GetBranchGeoHeatmapResult>
{
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IExecutiveFinancialMetricsService _financialMetrics;

    public GetBranchGeoHeatmapHandler(
        IOperationalAnalyticsScopeService scopeService,
        IExecutiveFinancialMetricsService financialMetrics)
    {
        _scopeService = scopeService;
        _financialMetrics = financialMetrics;
    }

    public async Task<GetBranchGeoHeatmapResult> Handle(
        GetBranchGeoHeatmapRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new GetBranchGeoHeatmapResult { ScopeLabelAr = scope.ScopeLabelAr };
        }

        var metrics = await _financialMetrics.ComputeAsync(
            scope.EffectiveBranchIds,
            DateTime.UtcNow,
            30,
            cancellationToken);

        var maxRevenue = metrics.BranchHeat.Count > 0
            ? metrics.BranchHeat.Max(b => b.Revenue)
            : 0m;

        var points = metrics.BranchHeat.Select(b =>
        {
            var (lat, lng) = SyriaBranchGeoCatalog.Resolve(b.BranchName, null);
            var heat = maxRevenue > 0
                ? (int)Math.Round((double)(b.Revenue / maxRevenue * 100m))
                : 0;
            return new BranchGeoHeatmapPointDto
            {
                BranchId = b.BranchId,
                BranchName = b.BranchName,
                Latitude = lat,
                Longitude = lng,
                Revenue = b.Revenue,
                ActiveSubscriptions = b.ActiveSubscriptions,
                HeatScore = heat,
            };
        }).ToList();

        return new GetBranchGeoHeatmapResult
        {
            ScopeLabelAr = scope.ScopeLabelAr,
            Points = points,
        };
    }
}
