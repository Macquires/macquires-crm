using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public sealed class StrategicMetricsTrendPointDto
{
    public string Label { get; init; } = null!;
    public decimal Value { get; init; }
}

public sealed class StrategicMetricsSegmentDto
{
    public string Segment { get; init; } = null!;
    public int Count { get; init; }
    public decimal Percent { get; init; }
}

public sealed class StrategicBranchLeaderboardRowDto
{
    public string BranchId { get; init; } = null!;
    public string BranchName { get; init; } = null!;
    public int ActiveSubscriptions { get; init; }
    public decimal RevenueContribution { get; init; }
    public decimal AvgResolutionHours { get; init; }
    public decimal ManagerRating { get; init; }
}

public sealed class StrategicRevenueCategoryDto
{
    public string Category { get; init; } = null!;
    public decimal Amount { get; init; }
    public decimal Percent { get; init; }
}

public class GetStrategicMetricsResult
{
    public decimal RevenueIndex { get; init; }
    public decimal Arpu { get; init; }
    public decimal RevenueChangePercent { get; init; }
    public decimal ChurnPercent30 { get; init; }
    public decimal ChurnPercent60 { get; init; }
    public int TicketsHandled { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public string? TopBranchName { get; init; }
    public int TopBranchRank { get; init; }

    public string AccessLevel { get; init; } = null!;
    public string ScopeLabelAr { get; init; } = null!;
    public bool CanUseFilters { get; init; }
    public string? EffectiveRegionId { get; init; }
    public string? EffectiveBranchId { get; init; }

    public IReadOnlyList<StrategicScopeOptionDto> Regions { get; init; } = [];
    public IReadOnlyList<StrategicScopeOptionDto> Branches { get; init; } = [];
    public IReadOnlyList<StrategicMetricsTrendPointDto> RevenueTrend { get; init; } = [];
    public IReadOnlyList<StrategicMetricsSegmentDto> SubscriberSegments { get; init; } = [];
    public IReadOnlyList<StrategicBranchLeaderboardRowDto> BranchLeaderboard { get; init; } = [];
    public IReadOnlyList<StrategicRevenueCategoryDto> RevenueByCategory { get; init; } = [];
}

public record GetStrategicMetricsRequest(string? RegionId, string? BranchId)
    : IRequest<GetStrategicMetricsResult>, IOperationalKpiRequest;

public class GetStrategicMetricsHandler : IRequestHandler<GetStrategicMetricsRequest, GetStrategicMetricsResult>
{
    private static readonly TimeSpan SlaCritical = TimeSpan.FromHours(4);
    private static readonly TimeSpan SlaHigh = TimeSpan.FromHours(8);
    private static readonly TimeSpan SlaNormal = TimeSpan.FromHours(24);

    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IExecutiveFinancialMetricsService _financialMetrics;
    private readonly IOperatorContext _operator;
    private readonly IUserAuditService _audit;

    public GetStrategicMetricsHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService,
        IExecutiveFinancialMetricsService financialMetrics,
        IOperatorContext operatorContext,
        IUserAuditService audit)
    {
        _context = context;
        _scopeService = scopeService;
        _financialMetrics = financialMetrics;
        _operator = operatorContext;
        _audit = audit;
    }

    public async Task<GetStrategicMetricsResult> Handle(
        GetStrategicMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _operator.UserId
            ?? throw new UnauthorizedAccessException("يجب تسجيل الدخول لعرض التحليلات الاستراتيجية.");

        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (scope.EffectiveBranchIds.Count == 0)
        {
            return EmptyResult(scope);
        }

        var branchIds = scope.EffectiveBranchIds;
        var metrics = await _financialMetrics.ComputeAsync(
            branchIds,
            DateTime.UtcNow,
            billingCycleDays: 30,
            cancellationToken);

        var customerIdsInScope = await _context.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.OrgUnitId != null && branchIds.Contains(c.OrgUnitId))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var branches = await _context.OrgUnit.AsNoTracking()
            .Where(x => branchIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var tickets = await LoadScopedTicketsAsync(customerIdsInScope, cancellationToken);
        var slaPercent = ComputeSlaCompliance(tickets);

        var leaderboard = await BuildLeaderboardAsync(
            branches, metrics.BranchHeat, customerIdsInScope, cancellationToken);
        var top = leaderboard.OrderByDescending(x => x.RevenueContribution).FirstOrDefault();

        var segments = await BuildSubscriberSegmentsAsync(customerIdsInScope, cancellationToken);

        await _audit.LogAsync(new UserAuditLogRequest
        {
            ActorUserId = userId,
            ActionType = UserAuditActionTypes.StrategicReportViewed,
            SummaryAr = $"عرض لوحة التحليلات الاستراتيجية — {scope.ScopeLabelAr}",
            Payload = new
            {
                scopeLabelAr = scope.ScopeLabelAr,
                accessLevel = scope.AccessLevel.ToString(),
                regionId = scope.EffectiveRegionId,
                branchId = scope.EffectiveBranchId,
                branchCount = branchIds.Count,
            },
        }, cancellationToken);

        return new GetStrategicMetricsResult
        {
            RevenueIndex = metrics.TotalRevenue,
            Arpu = metrics.Arpu,
            RevenueChangePercent = metrics.RevenueChangePercent,
            ChurnPercent30 = metrics.ChurnPercent30,
            ChurnPercent60 = metrics.ChurnPercent60,
            TicketsHandled = tickets.Count,
            SlaCompliancePercent = slaPercent,
            TopBranchName = top?.BranchName,
            TopBranchRank = top != null ? 1 : 0,
            AccessLevel = scope.AccessLevel.ToString(),
            ScopeLabelAr = scope.ScopeLabelAr,
            CanUseFilters = scope.CanUseFilters,
            EffectiveRegionId = scope.EffectiveRegionId,
            EffectiveBranchId = scope.EffectiveBranchId,
            Regions = scope.Regions,
            Branches = scope.Branches,
            RevenueTrend = metrics.RevenueTrend
                .Select(t => new StrategicMetricsTrendPointDto { Label = t.Label, Value = t.Value })
                .ToList(),
            SubscriberSegments = segments,
            BranchLeaderboard = leaderboard,
            RevenueByCategory = metrics.RevenueByCategory
                .Select(c => new StrategicRevenueCategoryDto
                {
                    Category = c.Category,
                    Amount = c.Amount,
                    Percent = c.Percent,
                })
                .ToList(),
        };
    }

    private static GetStrategicMetricsResult EmptyResult(StrategicDataScope scope) =>
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

    private async Task<List<TelecomTechnicalTicket>> LoadScopedTicketsAsync(
        List<string> customerIdsInScope,
        CancellationToken cancellationToken)
    {
        if (customerIdsInScope.Count == 0)
        {
            return [];
        }

        return await _context.TelecomTechnicalTicket
            .AsNoTracking()
            .Where(t => !t.IsDeleted
                && t.CustomerId != null
                && customerIdsInScope.Contains(t.CustomerId))
            .ToListAsync(cancellationToken);
    }

    private static decimal ComputeSlaCompliance(List<TelecomTechnicalTicket> tickets)
    {
        var resolved = tickets
            .Where(t => t.Status == TechnicalTicketStatus.Resolved && t.ResolvedAtUtc.HasValue && t.CreatedAtUtc.HasValue)
            .ToList();

        if (resolved.Count == 0)
        {
            return 100m;
        }

        var met = resolved.Count(t =>
        {
            var elapsed = t.ResolvedAtUtc!.Value - t.CreatedAtUtc!.Value;
            var target = t.Priority switch
            {
                TechnicalTicketPriority.Critical => SlaCritical,
                TechnicalTicketPriority.High => SlaHigh,
                _ => SlaNormal,
            };
            return elapsed <= target;
        });

        return decimal.Round((decimal)met / resolved.Count * 100m, 1);
    }

    private async Task<List<StrategicBranchLeaderboardRowDto>> BuildLeaderboardAsync(
        List<OrgUnit> branches,
        IReadOnlyList<ExecutiveBranchHeatDto> branchHeat,
        List<string> customerIdsInScope,
        CancellationToken cancellationToken)
    {
        var heatByBranch = branchHeat.ToDictionary(h => h.BranchId, StringComparer.Ordinal);
        var maxRevenue = branchHeat.Count > 0 ? branchHeat.Max(h => h.Revenue) : 0m;
        var rows = new List<StrategicBranchLeaderboardRowDto>();

        foreach (var branch in branches)
        {
            var customerIds = await _context.Customer.AsNoTracking()
                .Where(c => !c.IsDeleted && c.OrgUnitId == branch.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var branchTickets = await _context.TelecomTechnicalTicket
                .AsNoTracking()
                .Where(t => !t.IsDeleted
                    && t.CustomerId != null
                    && customerIds.Contains(t.CustomerId)
                    && t.Status == TechnicalTicketStatus.Resolved
                    && t.ResolvedAtUtc != null
                    && t.CreatedAtUtc != null)
                .ToListAsync(cancellationToken);

            var avgHours = branchTickets.Count == 0
                ? 0m
                : decimal.Round((decimal)branchTickets.Average(t =>
                    (t.ResolvedAtUtc!.Value - t.CreatedAtUtc!.Value).TotalHours), 1);

            var heat = heatByBranch.GetValueOrDefault(branch.Id);
            var revenue = heat?.Revenue ?? 0m;
            var subs = heat?.ActiveSubscriptions ?? 0;

            var revenueScore = maxRevenue > 0 ? revenue / maxRevenue * 2.5m : 0m;
            var slaScore = avgHours > 0 ? Math.Min(2.5m, Math.Max(0, 24 - avgHours) / 24 * 2.5m) : 1.5m;
            var rating = decimal.Round(Math.Min(5m, revenueScore + slaScore), 1);

            rows.Add(new StrategicBranchLeaderboardRowDto
            {
                BranchId = branch.Id,
                BranchName = branch.NameAr,
                ActiveSubscriptions = subs,
                RevenueContribution = revenue,
                AvgResolutionHours = avgHours,
                ManagerRating = rating,
            });
        }

        return rows.OrderByDescending(r => r.RevenueContribution).ToList();
    }

    private async Task<List<StrategicMetricsSegmentDto>> BuildSubscriberSegmentsAsync(
        List<string> customerIdsInScope,
        CancellationToken cancellationToken)
    {
        if (customerIdsInScope.Count == 0)
        {
            return
            [
                new() { Segment = "Platinum", Count = 0, Percent = 0 },
                new() { Segment = "Gold", Count = 0, Percent = 0 },
                new() { Segment = "Silver", Count = 0, Percent = 0 },
            ];
        }

        var categoryCounts = await (
            from s in _context.TelecomSubscription.AsNoTracking()
            join p in _context.SubscriberProfile.AsNoTracking() on s.SubscriberProfileId equals p.Id
            join m in _context.MsisdnAsset.AsNoTracking() on s.MsisdnAssetId equals m.Id
            where !s.IsDeleted && !p.IsDeleted && !m.IsDeleted
                && customerIdsInScope.Contains(p.CustomerId)
            group m by m.Category into g
            select new { Category = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var platinum = categoryCounts.FirstOrDefault(x => x.Category == MsisdnCategory.Platinum)?.Count ?? 0;
        var gold = categoryCounts.FirstOrDefault(x => x.Category == MsisdnCategory.Gold)?.Count ?? 0;
        var silver = categoryCounts.FirstOrDefault(x => x.Category == MsisdnCategory.Silver)?.Count ?? 0;
        var normal = categoryCounts.FirstOrDefault(x => x.Category == MsisdnCategory.Normal)?.Count ?? 0;
        silver += normal;

        var total = platinum + gold + silver;
        if (total == 0)
        {
            total = 1;
        }

        return
        [
            new() { Segment = "Platinum", Count = platinum, Percent = decimal.Round((decimal)platinum / total * 100m, 1) },
            new() { Segment = "Gold", Count = gold, Percent = decimal.Round((decimal)gold / total * 100m, 1) },
            new() { Segment = "Silver", Count = silver, Percent = decimal.Round((decimal)silver / total * 100m, 1) },
        ];
    }
}
