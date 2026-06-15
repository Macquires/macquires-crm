using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public sealed class ExecutiveCommandCenterBranchHighlightDto
{
    public string BranchId { get; init; } = null!;
    public string BranchName { get; init; } = null!;
    public decimal Revenue { get; init; }
    public int ActiveSubscriptions { get; init; }
}

public class GetExecutiveCommandCenterSummaryResult
{
    public decimal TotalRevenue { get; init; }
    public decimal RevenueChangePercent { get; init; }
    public decimal Arpu { get; init; }
    public decimal ChurnPercent30 { get; init; }
    public decimal ChurnPercent60 { get; init; }
    public int OperationsToday { get; init; }
    public int OpenTickets { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public int CriticalAlerts { get; init; }
    public int WarningAlerts { get; init; }
    public int OnlineEmployees { get; init; }
    public ExecutiveCommandCenterBranchHighlightDto? BestBranch { get; init; }
    public ExecutiveCommandCenterBranchHighlightDto? WeakestBranch { get; init; }
    public string? WeeklyDigestSummaryAr { get; init; }

    public string AccessLevel { get; init; } = null!;
    public string ScopeLabelAr { get; init; } = null!;
    public bool CanUseFilters { get; init; }
    public string? EffectiveRegionId { get; init; }
    public string? EffectiveBranchId { get; init; }
    public IReadOnlyList<StrategicScopeOptionDto> Regions { get; init; } = [];
    public IReadOnlyList<StrategicScopeOptionDto> Branches { get; init; } = [];
}

public record GetExecutiveCommandCenterSummaryRequest(
    string? RegionId,
    string? BranchId,
    DateTime? FromUtc,
    DateTime? ToUtc)
    : IRequest<GetExecutiveCommandCenterSummaryResult>, IOperationalKpiRequest;

public class GetExecutiveCommandCenterSummaryHandler
    : IRequestHandler<GetExecutiveCommandCenterSummaryRequest, GetExecutiveCommandCenterSummaryResult>
{
    private static readonly TimeSpan SlaCritical = TimeSpan.FromHours(4);
    private static readonly TimeSpan SlaHigh = TimeSpan.FromHours(8);
    private static readonly TimeSpan SlaNormal = TimeSpan.FromHours(24);

    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IExecutiveFinancialMetricsService _financialMetrics;
    private readonly IWorkforceUserReadService _workforceUsers;
    private readonly IOperatorContext _operator;
    private readonly IUserAuditService _audit;
    private readonly IMediator _mediator;

    public GetExecutiveCommandCenterSummaryHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService,
        IExecutiveFinancialMetricsService financialMetrics,
        IWorkforceUserReadService workforceUsers,
        IOperatorContext operatorContext,
        IUserAuditService audit,
        IMediator mediator)
    {
        _context = context;
        _scopeService = scopeService;
        _financialMetrics = financialMetrics;
        _workforceUsers = workforceUsers;
        _operator = operatorContext;
        _audit = audit;
        _mediator = mediator;
    }

    public async Task<GetExecutiveCommandCenterSummaryResult> Handle(
        GetExecutiveCommandCenterSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _operator.UserId
            ?? throw new UnauthorizedAccessException("يجب تسجيل الدخول لعرض مركز القيادة التنفيذية.");

        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (scope.EffectiveBranchIds.Count == 0)
        {
            return Empty(scope);
        }

        var branchIds = scope.EffectiveBranchIds;
        var now = DateTime.UtcNow;
        var from = request.FromUtc ?? now.Date;
        var to = request.ToUtc ?? now;

        var metrics = await _financialMetrics.ComputeAsync(
            branchIds,
            to,
            billingCycleDays: 30,
            cancellationToken);

        var operationsToday = await _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(branchIds)
            .CountAsync(
                o => !o.IsDeleted && o.CreatedAtUtc >= from && o.CreatedAtUtc <= to,
                cancellationToken);

        var customerIdsInScope = await _context.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.OrgUnitId != null && branchIds.Contains(c.OrgUnitId))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var openTickets = customerIdsInScope.Count == 0
            ? 0
            : await _context.TelecomTechnicalTicket.AsNoTracking()
                .CountAsync(
                    t => !t.IsDeleted
                        && t.CustomerId != null
                        && customerIdsInScope.Contains(t.CustomerId)
                        && t.Status != TechnicalTicketStatus.Resolved,
                    cancellationToken);

        var slaPercent = customerIdsInScope.Count == 0
            ? 100m
            : await ComputeSlaComplianceAsync(customerIdsInScope, cancellationToken);

        var exceptions = await _mediator.Send(
            new GetExecutiveExceptionsRequest(request.RegionId, request.BranchId),
            cancellationToken);

        var onlineEmployees = await _workforceUsers.CountOnlineInBranchesAsync(branchIds, cancellationToken);

        var heat = metrics.BranchHeat
            .OrderByDescending(b => b.Revenue)
            .ToList();

        ExecutiveCommandCenterBranchHighlightDto? best = null;
        ExecutiveCommandCenterBranchHighlightDto? weakest = null;
        if (heat.Count > 0)
        {
            var top = heat[0];
            best = new ExecutiveCommandCenterBranchHighlightDto
            {
                BranchId = top.BranchId,
                BranchName = top.BranchName,
                Revenue = top.Revenue,
                ActiveSubscriptions = top.ActiveSubscriptions,
            };

            var bottom = heat[^1];
            if (heat.Count > 1)
            {
                weakest = new ExecutiveCommandCenterBranchHighlightDto
                {
                    BranchId = bottom.BranchId,
                    BranchName = bottom.BranchName,
                    Revenue = bottom.Revenue,
                    ActiveSubscriptions = bottom.ActiveSubscriptions,
                };
            }
        }

        string? digestSummary = null;
        try
        {
            var digest = await _mediator.Send(
                new GetExecutiveWeeklyDigestRequest(request.RegionId, request.BranchId),
                cancellationToken);
            digestSummary = digest.SummaryAr;
        }
        catch
        {
            // optional enrichment
        }

        await _audit.LogAsync(new UserAuditLogRequest
        {
            ActorUserId = userId,
            ActionType = UserAuditActionTypes.StrategicReportViewed,
            SummaryAr = $"عرض مركز القيادة التنفيذية — {scope.ScopeLabelAr}",
            Payload = new
            {
                report = "executive-command-center",
                scopeLabelAr = scope.ScopeLabelAr,
                accessLevel = scope.AccessLevel.ToString(),
            },
        }, cancellationToken);

        return new GetExecutiveCommandCenterSummaryResult
        {
            TotalRevenue = metrics.TotalRevenue,
            RevenueChangePercent = metrics.RevenueChangePercent,
            Arpu = metrics.Arpu,
            ChurnPercent30 = metrics.ChurnPercent30,
            ChurnPercent60 = metrics.ChurnPercent60,
            OperationsToday = operationsToday,
            OpenTickets = openTickets,
            SlaCompliancePercent = slaPercent,
            CriticalAlerts = exceptions.CriticalCount,
            WarningAlerts = exceptions.WarningCount,
            OnlineEmployees = onlineEmployees,
            BestBranch = best,
            WeakestBranch = weakest,
            WeeklyDigestSummaryAr = digestSummary,
            AccessLevel = scope.AccessLevel.ToString(),
            ScopeLabelAr = scope.ScopeLabelAr,
            CanUseFilters = scope.CanUseFilters,
            EffectiveRegionId = scope.EffectiveRegionId,
            EffectiveBranchId = scope.EffectiveBranchId,
            Regions = scope.Regions,
            Branches = scope.Branches,
        };
    }

    private async Task<decimal> ComputeSlaComplianceAsync(
        List<string> customerIdsInScope,
        CancellationToken cancellationToken)
    {
        var tickets = await _context.TelecomTechnicalTicket.AsNoTracking()
            .Where(t => !t.IsDeleted
                && t.CustomerId != null
                && customerIdsInScope.Contains(t.CustomerId)
                && t.Status == TechnicalTicketStatus.Resolved
                && t.ResolvedAtUtc.HasValue
                && t.CreatedAtUtc.HasValue)
            .Select(t => new { t.Priority, t.CreatedAtUtc, t.ResolvedAtUtc })
            .ToListAsync(cancellationToken);

        if (tickets.Count == 0)
        {
            return 100m;
        }

        var met = tickets.Count(t =>
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

        return Math.Round((decimal)met / tickets.Count * 100m, 2);
    }

    private static GetExecutiveCommandCenterSummaryResult Empty(StrategicDataScope scope) =>
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
