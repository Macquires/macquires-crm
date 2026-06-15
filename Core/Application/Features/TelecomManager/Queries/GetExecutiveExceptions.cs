using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public enum ExecutiveExceptionSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2,
}

public sealed class ExecutiveExceptionItemDto
{
    public string Code { get; init; } = null!;
    public ExecutiveExceptionSeverity Severity { get; init; }
    public string TitleAr { get; init; } = null!;
    public string DetailAr { get; init; } = null!;
    public string? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string? ActionUrl { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

public class GetExecutiveExceptionsResult
{
    public int TotalCount { get; init; }
    public int CriticalCount { get; init; }
    public int WarningCount { get; init; }
    public string ScopeLabelAr { get; init; } = null!;
    public IReadOnlyList<ExecutiveExceptionItemDto> Items { get; init; } = [];
}

public record GetExecutiveExceptionsRequest(string? RegionId, string? BranchId)
    : IRequest<GetExecutiveExceptionsResult>, IOperationalKpiRequest;

public class GetExecutiveExceptionsHandler : IRequestHandler<GetExecutiveExceptionsRequest, GetExecutiveExceptionsResult>
{
    private const decimal RevenueDropThresholdPercent = 15m;
    private const decimal IntegrationHealthMinPercent = 90m;
    private const int SupervisorOverrideThreshold = 3;
    private const int PendingOpsThreshold = 25;

    private static readonly TimeSpan SlaCritical = TimeSpan.FromHours(4);
    private static readonly TimeSpan SlaHigh = TimeSpan.FromHours(8);

    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IExecutiveFinancialMetricsService _financialMetrics;
    private readonly IExecutiveAlertBroadcaster? _alertBroadcaster;

    public GetExecutiveExceptionsHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService,
        IExecutiveFinancialMetricsService financialMetrics,
        IExecutiveAlertBroadcaster? alertBroadcaster = null)
    {
        _context = context;
        _scopeService = scopeService;
        _financialMetrics = financialMetrics;
        _alertBroadcaster = alertBroadcaster;
    }

    public async Task<GetExecutiveExceptionsResult> Handle(
        GetExecutiveExceptionsRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new GetExecutiveExceptionsResult { ScopeLabelAr = scope.ScopeLabelAr };
        }

        var branchIds = scope.EffectiveBranchIds;
        var branchNames = await _context.OrgUnit.AsNoTracking()
            .Where(o => branchIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.NameAr, cancellationToken);

        var items = new List<ExecutiveExceptionItemDto>();
        var now = DateTime.UtcNow;

        await AppendIntegrationExceptionsAsync(items, cancellationToken);
        await AppendSlaBreachesAsync(items, branchIds, branchNames, now, cancellationToken);
        await AppendRevenueDropExceptionsAsync(items, branchIds, branchNames, now, cancellationToken);
        await AppendSupervisorOverrideExceptionsAsync(items, branchIds, branchNames, now, cancellationToken);
        await AppendPendingOpsExceptionsAsync(items, branchIds, branchNames, cancellationToken);

        var ordered = items
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.OccurredAtUtc ?? DateTime.MinValue)
            .Take(40)
            .ToList();

        var result = new GetExecutiveExceptionsResult
        {
            TotalCount = ordered.Count,
            CriticalCount = ordered.Count(i => i.Severity == ExecutiveExceptionSeverity.Critical),
            WarningCount = ordered.Count(i => i.Severity == ExecutiveExceptionSeverity.Warning),
            ScopeLabelAr = scope.ScopeLabelAr,
            Items = ordered,
        };

        if (_alertBroadcaster != null && result.CriticalCount > 0)
        {
            await _alertBroadcaster.BroadcastCriticalCountAsync(
                result.CriticalCount,
                result.WarningCount,
                result.ScopeLabelAr,
                cancellationToken);
        }

        return result;
    }

    private async Task AppendIntegrationExceptionsAsync(
        List<ExecutiveExceptionItemDto> items,
        CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var logs = await _context.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
            .Where(l => l.CreatedAtUtc >= since)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            return;
        }

        var success = logs.Count(l => l.Success);
        var pct = (decimal)success / logs.Count * 100m;
        if (pct < IntegrationHealthMinPercent)
        {
            items.Add(new ExecutiveExceptionItemDto
            {
                Code = "integration_health_low",
                Severity = pct < 70m ? ExecutiveExceptionSeverity.Critical : ExecutiveExceptionSeverity.Warning,
                TitleAr = "انخفاض نبض التكامل",
                DetailAr = $"نجاح التكامل {pct:F0}% خلال 24 ساعة ({success}/{logs.Count}).",
                ActionUrl = "/Telecom/BillingIntegration",
                OccurredAtUtc = logs.Max(l => l.CreatedAtUtc),
            });
        }
    }

    private async Task AppendSlaBreachesAsync(
        List<ExecutiveExceptionItemDto> items,
        IReadOnlyList<string> branchIds,
        Dictionary<string, string> branchNames,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var openTickets = await _context.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.BranchId != null
                && branchIds.Contains(t.BranchId)
                && (t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress)
                && t.CreatedAtUtc != null)
            .ToListAsync(cancellationToken);

        foreach (var ticket in openTickets)
        {
            var elapsed = now - ticket.CreatedAtUtc!.Value;
            var target = ticket.Priority switch
            {
                TechnicalTicketPriority.Critical => SlaCritical,
                TechnicalTicketPriority.High => SlaHigh,
                _ => TimeSpan.FromHours(24),
            };

            if (elapsed <= target)
            {
                continue;
            }

            var branchName = ticket.BranchId != null ? branchNames.GetValueOrDefault(ticket.BranchId) : null;
            items.Add(new ExecutiveExceptionItemDto
            {
                Code = "ticket_sla_breach",
                Severity = ticket.Priority == TechnicalTicketPriority.Critical
                    ? ExecutiveExceptionSeverity.Critical
                    : ExecutiveExceptionSeverity.Warning,
                TitleAr = "تجاوز SLA تذكرة فنية",
                DetailAr = $"{ticket.TicketNumber} — {branchName ?? "فرع"} — {(int)elapsed.TotalHours}س مفتوحة.",
                BranchId = ticket.BranchId,
                BranchName = branchName,
                ActionUrl = "/Telecom/TechnicalTicketList",
                OccurredAtUtc = ticket.CreatedAtUtc,
            });
        }
    }

    private async Task AppendRevenueDropExceptionsAsync(
        List<ExecutiveExceptionItemDto> items,
        IReadOnlyList<string> branchIds,
        Dictionary<string, string> branchNames,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var current = await _financialMetrics.ComputeAsync(branchIds, now, 30, cancellationToken);
        var prior = await _financialMetrics.ComputeAsync(branchIds, now.AddDays(-30), 30, cancellationToken);

        var priorByBranch = prior.BranchHeat.ToDictionary(b => b.BranchId, b => b.Revenue);
        foreach (var branch in current.BranchHeat)
        {
            if (!priorByBranch.TryGetValue(branch.BranchId, out var priorRevenue) || priorRevenue <= 0)
            {
                continue;
            }

            var drop = (priorRevenue - branch.Revenue) / priorRevenue * 100m;
            if (drop < RevenueDropThresholdPercent)
            {
                continue;
            }

            items.Add(new ExecutiveExceptionItemDto
            {
                Code = "branch_revenue_drop",
                Severity = drop >= 30m ? ExecutiveExceptionSeverity.Critical : ExecutiveExceptionSeverity.Warning,
                TitleAr = "انخفاض إيراد فرع",
                DetailAr = $"{branch.BranchName}: انخفاض {drop:F1}% مقارنة بالفترة السابقة.",
                BranchId = branch.BranchId,
                BranchName = branch.BranchName,
                ActionUrl = "/Dashboards/DefaultDashboard#strategic-analytics",
                OccurredAtUtc = now,
            });
        }
    }

    private async Task AppendSupervisorOverrideExceptionsAsync(
        List<ExecutiveExceptionItemDto> items,
        IReadOnlyList<string> branchIds,
        Dictionary<string, string> branchNames,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var since = now.AddDays(-7);
        var overrides = await _context.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.BranchId != null
                && branchIds.Contains(o.BranchId)
                && o.CreatedAtUtc >= since
                && o.OverrideReasonCode != null)
            .GroupBy(o => o.BranchId!)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in overrides.Where(x => x.Count >= SupervisorOverrideThreshold))
        {
            var name = branchNames.GetValueOrDefault(row.BranchId);
            items.Add(new ExecutiveExceptionItemDto
            {
                Code = "supervisor_override_spike",
                Severity = row.Count >= 8 ? ExecutiveExceptionSeverity.Critical : ExecutiveExceptionSeverity.Warning,
                TitleAr = "تجاوزات مشرف متكررة",
                DetailAr = $"{name ?? row.BranchId}: {row.Count} عملية Override خلال 7 أيام.",
                BranchId = row.BranchId,
                BranchName = name,
                ActionUrl = "/Telecom/TelecomHub",
                OccurredAtUtc = now,
            });
        }
    }

    private async Task AppendPendingOpsExceptionsAsync(
        List<ExecutiveExceptionItemDto> items,
        IReadOnlyList<string> branchIds,
        Dictionary<string, string> branchNames,
        CancellationToken cancellationToken)
    {
        var pending = await _context.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.BranchId != null
                && branchIds.Contains(o.BranchId)
                && (o.Status == TelecomOperationStatus.PendingDocuments
                    || o.Status == TelecomOperationStatus.Confirmed
                    || o.Status == TelecomOperationStatus.PendingExternal))
            .GroupBy(o => o.BranchId!)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in pending.Where(x => x.Count >= PendingOpsThreshold))
        {
            var name = branchNames.GetValueOrDefault(row.BranchId);
            items.Add(new ExecutiveExceptionItemDto
            {
                Code = "pending_ops_backlog",
                Severity = row.Count >= 50 ? ExecutiveExceptionSeverity.Critical : ExecutiveExceptionSeverity.Warning,
                TitleAr = "تراكم عمليات معلقة",
                DetailAr = $"{name ?? row.BranchId}: {row.Count} عملية بانتظار الإنجاز.",
                BranchId = row.BranchId,
                BranchName = name,
                ActionUrl = "/Telecom/TelecomHub",
            });
        }
    }
}
