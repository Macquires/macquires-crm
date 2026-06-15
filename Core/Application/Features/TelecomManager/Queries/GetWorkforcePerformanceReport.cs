using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public enum WorkforceRoleFilter
{
    All = 0,
    Showroom = 1,
    BackOffice = 2,
    CallCenter = 3,
}

public sealed class WorkforcePerformanceRowDto
{
    public string UserId { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string? OrgUnitName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public bool IsOnline { get; init; }
    public DateTime? LastActivityUtc { get; init; }
    public int OperationsCreated { get; init; }
    public int OperationsConfirmed { get; init; }
    public int PaymentsCompleted { get; init; }
    public int TicketsOpened { get; init; }
    public int TicketsResolved { get; init; }
    public int AuditActionsCount { get; init; }
    public int SupervisorInterventions { get; init; }
    public decimal AvgTicketResolutionHours { get; init; }
    public decimal ProductivityScore { get; init; }
}

public class GetWorkforcePerformanceReportResult
{
    public IReadOnlyList<WorkforcePerformanceRowDto> Rows { get; init; } = [];
    public string ScopeLabelAr { get; init; } = null!;
    public string AccessLevel { get; init; } = null!;
}

public record GetWorkforcePerformanceReportRequest(
    string? RegionId,
    string? BranchId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    WorkforceRoleFilter? RoleFilter)
    : IRequest<GetWorkforcePerformanceReportResult>, IOperationalKpiRequest;

public class GetWorkforcePerformanceReportHandler
    : IRequestHandler<GetWorkforcePerformanceReportRequest, GetWorkforcePerformanceReportResult>
{
    private readonly IQueryContext _context;
    private readonly IWorkforceAnalyticsScopeService _workforceScope;
    private readonly IUserAuditReadService _auditRead;
    private readonly IOperatorContext _operator;
    private readonly IUserAuditService _audit;

    public GetWorkforcePerformanceReportHandler(
        IQueryContext context,
        IWorkforceAnalyticsScopeService workforceScope,
        IUserAuditReadService auditRead,
        IOperatorContext operatorContext,
        IUserAuditService audit)
    {
        _context = context;
        _workforceScope = workforceScope;
        _auditRead = auditRead;
        _operator = operatorContext;
        _audit = audit;
    }

    public async Task<GetWorkforcePerformanceReportResult> Handle(
        GetWorkforcePerformanceReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _operator.UserId
            ?? throw new UnauthorizedAccessException("يجب تسجيل الدخول لعرض تقرير أداء الموظفين.");

        var scope = await _workforceScope.ResolveAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        var dataScope = scope.DataScope;
        if (dataScope.EffectiveBranchIds.Count == 0 || scope.Users.Count == 0)
        {
            return new GetWorkforcePerformanceReportResult
            {
                ScopeLabelAr = dataScope.ScopeLabelAr,
                AccessLevel = dataScope.AccessLevel.ToString(),
            };
        }

        var from = request.FromUtc ?? DateTime.UtcNow.AddDays(-30);
        var to = request.ToUtc ?? DateTime.UtcNow;
        var branchIds = dataScope.EffectiveBranchIds;
        var users = FilterByRole(scope.Users, request.RoleFilter);
        var userIds = users.Select(u => u.UserId).ToList();

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(branchIds)
            .Where(o => !o.IsDeleted && o.CreatedAtUtc >= from && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.CreatedById,
                o.UpdatedById,
                o.ClaimedByUserId,
                o.Status,
                o.OverrideReasonCode,
                o.BarringLevel,
                o.ApprovalLevelRequired,
            })
            .ToListAsync(cancellationToken);

        var payments = await _context.TelecomPaymentTransaction.AsNoTracking()
            .InBranchScope(branchIds)
            .Where(p => !p.IsDeleted
                && p.Status == PaymentTransactionStatus.Completed
                && p.CreatedAtUtc >= from
                && p.CreatedAtUtc <= to
                && p.CreatedById != null)
            .GroupBy(p => p.CreatedById!)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var ticketsOpened = await _context.TelecomTechnicalTicket.AsNoTracking()
            .Where(t => !t.IsDeleted
                && t.CreatedAtUtc >= from
                && t.CreatedAtUtc <= to
                && userIds.Contains(t.OpenedByUserId))
            .GroupBy(t => t.OpenedByUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var ticketsResolved = await _context.TelecomTechnicalTicket.AsNoTracking()
            .Where(t => !t.IsDeleted
                && t.ResolvedAtUtc >= from
                && t.ResolvedAtUtc <= to
                && t.ResolvedByUserId != null
                && userIds.Contains(t.ResolvedByUserId))
            .Select(t => new { UserId = t.ResolvedByUserId!, t.CreatedAtUtc, t.ResolvedAtUtc })
            .ToListAsync(cancellationToken);

        var auditCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var uid in userIds)
        {
            var count = await _auditRead.CountAsync(new UserAuditLogQuery
            {
                ActorUserId = uid,
                FromUtc = from,
                ToUtc = to,
            }, cancellationToken);
            auditCounts[uid] = count;
        }

        var paymentByUser = payments.ToDictionary(x => x.UserId, x => x.Count);
        var openedByUser = ticketsOpened.ToDictionary(x => x.UserId, x => x.Count);

        var rows = users.Select(u =>
        {
            var uid = u.UserId;
            var created = ops.Count(o => o.CreatedById == uid);
            var confirmed = ops.Count(o =>
                (o.UpdatedById == uid || o.ClaimedByUserId == uid)
                && (o.Status == TelecomOperationStatus.Completed || o.Status == TelecomOperationStatus.Confirmed));
            var interventions = ops.Count(o =>
                (o.CreatedById == uid || o.UpdatedById == uid)
                && (!string.IsNullOrEmpty(o.OverrideReasonCode)
                    || !string.IsNullOrEmpty(o.BarringLevel)
                    || o.ApprovalLevelRequired == "BackOffice"));
            var resolvedTickets = ticketsResolved.Where(t => t.UserId == uid).ToList();
            var resolvedCount = resolvedTickets.Count;
            decimal avgHours = 0;
            if (resolvedCount > 0)
            {
                avgHours = Math.Round((decimal)resolvedTickets
                    .Where(t => t.CreatedAtUtc.HasValue && t.ResolvedAtUtc.HasValue)
                    .Select(t => (t.ResolvedAtUtc!.Value - t.CreatedAtUtc!.Value).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average(), 2);
            }

            paymentByUser.TryGetValue(uid, out var payCount);
            openedByUser.TryGetValue(uid, out var openedCount);
            auditCounts.TryGetValue(uid, out var auditCount);

            var score = ComputeProductivityScore(
                created,
                confirmed,
                payCount,
                openedCount,
                resolvedCount,
                auditCount,
                interventions);

            return new WorkforcePerformanceRowDto
            {
                UserId = uid,
                DisplayName = u.DisplayName,
                OrgUnitName = u.OrgUnitNameAr,
                Roles = u.Roles,
                IsOnline = u.IsOnline,
                LastActivityUtc = u.LastActivityUtc,
                OperationsCreated = created,
                OperationsConfirmed = confirmed,
                PaymentsCompleted = payCount,
                TicketsOpened = openedCount,
                TicketsResolved = resolvedCount,
                AuditActionsCount = auditCount,
                SupervisorInterventions = interventions,
                AvgTicketResolutionHours = avgHours,
                ProductivityScore = score,
            };
        })
        .OrderByDescending(r => r.ProductivityScore)
        .ThenBy(r => r.DisplayName)
        .ToList();

        await _audit.LogAsync(new UserAuditLogRequest
        {
            ActorUserId = userId,
            ActionType = UserAuditActionTypes.StrategicReportViewed,
            SummaryAr = $"عرض تقرير أداء الموظفين — {dataScope.ScopeLabelAr}",
            Payload = new { report = "workforce-performance", employeeCount = rows.Count },
        }, cancellationToken);

        return new GetWorkforcePerformanceReportResult
        {
            Rows = rows,
            ScopeLabelAr = dataScope.ScopeLabelAr,
            AccessLevel = dataScope.AccessLevel.ToString(),
        };
    }

    private static IReadOnlyList<WorkforceUserSnapshot> FilterByRole(
        IReadOnlyList<WorkforceUserSnapshot> users,
        WorkforceRoleFilter? filter)
    {
        if (!filter.HasValue || filter == WorkforceRoleFilter.All)
        {
            return users;
        }

        var roleName = filter switch
        {
            WorkforceRoleFilter.Showroom => "TelecomShowroom",
            WorkforceRoleFilter.BackOffice => "TelecomBackOffice",
            WorkforceRoleFilter.CallCenter => "TelecomCallCenter",
            _ => null,
        };

        if (roleName == null)
        {
            return users;
        }

        return users
            .Where(u => u.Roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static decimal ComputeProductivityScore(
        int created,
        int confirmed,
        int payments,
        int ticketsOpened,
        int ticketsResolved,
        int auditActions,
        int interventions)
    {
        var raw = created * 2m
            + confirmed * 3m
            + payments * 2m
            + ticketsOpened * 1m
            + ticketsResolved * 4m
            + auditActions * 1m
            - interventions * 2m;

        if (raw <= 0)
        {
            return 0m;
        }

        return Math.Min(100m, Math.Round(raw, 2));
    }
}
