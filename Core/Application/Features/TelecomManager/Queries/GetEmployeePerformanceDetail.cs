using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public sealed class EmployeePerformanceKpiDto
{
    public int OperationsCreated7d { get; init; }
    public int OperationsCreated30d { get; init; }
    public int TicketsResolved7d { get; init; }
    public int TicketsResolved30d { get; init; }
    public int PaymentsCompleted30d { get; init; }
    public decimal ProductivityScore30d { get; init; }
}

public sealed class EmployeePerformanceTimelineItemDto
{
    public DateTime OccurredAtUtc { get; init; }
    public string Kind { get; init; } = null!;
    public string TitleAr { get; init; } = null!;
    public string? Subtitle { get; init; }
    public string? ReferenceId { get; init; }
    public string? ActionUrl { get; init; }
}

public class GetEmployeePerformanceDetailResult
{
    public string UserId { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string? OrgUnitName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public bool IsOnline { get; init; }
    public DateTime? LastActivityUtc { get; init; }
    public EmployeePerformanceKpiDto Kpis { get; init; } = new();
    public IReadOnlyList<EmployeePerformanceTimelineItemDto> Timeline { get; init; } = [];
}

public record GetEmployeePerformanceDetailRequest(
    string UserId,
    string? RegionId,
    string? BranchId)
    : IRequest<GetEmployeePerformanceDetailResult>, IOperationalKpiRequest;

public class GetEmployeePerformanceDetailHandler
    : IRequestHandler<GetEmployeePerformanceDetailRequest, GetEmployeePerformanceDetailResult>
{
    private readonly IQueryContext _context;
    private readonly IWorkforceAnalyticsScopeService _workforceScope;
    private readonly IUserAuditReadService _auditRead;

    public GetEmployeePerformanceDetailHandler(
        IQueryContext context,
        IWorkforceAnalyticsScopeService workforceScope,
        IUserAuditReadService auditRead)
    {
        _context = context;
        _workforceScope = workforceScope;
        _auditRead = auditRead;
    }

    public async Task<GetEmployeePerformanceDetailResult> Handle(
        GetEmployeePerformanceDetailRequest request,
        CancellationToken cancellationToken)
    {
        var targetUserId = request.UserId.Trim();
        if (string.IsNullOrEmpty(targetUserId))
        {
            throw new ArgumentException("معرّف الموظف مطلوب.");
        }

        var scope = await _workforceScope.ResolveAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (!scope.AllowedUserIds.Contains(targetUserId, StringComparer.Ordinal))
        {
            throw new UnauthorizedAccessException("لا صلاحية لعرض أداء هذا الموظف ضمن النطاق الحالي.");
        }

        var employee = scope.Users.FirstOrDefault(u => u.UserId == targetUserId)
            ?? throw new KeyNotFoundException("الموظف غير موجود ضمن النطاق.");

        var now = DateTime.UtcNow;
        var from7 = now.AddDays(-7);
        var from30 = now.AddDays(-30);
        var branchIds = scope.DataScope.EffectiveBranchIds;

        var ops30 = await _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(branchIds)
            .Where(o => !o.IsDeleted && o.CreatedById == targetUserId && o.CreatedAtUtc >= from30)
            .Select(o => new { o.Id, o.Number, o.Kind, o.Status, o.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        var tickets30 = await _context.TelecomTechnicalTicket.AsNoTracking()
            .Where(t => !t.IsDeleted
                && t.ResolvedByUserId == targetUserId
                && t.ResolvedAtUtc >= from30)
            .Select(t => new { t.Id, t.TicketNumber, t.ResolvedAtUtc, t.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        var payments30 = await _context.TelecomPaymentTransaction.AsNoTracking()
            .InBranchScope(branchIds)
            .CountAsync(
                p => !p.IsDeleted
                    && p.CreatedById == targetUserId
                    && p.Status == PaymentTransactionStatus.Completed
                    && p.CreatedAtUtc >= from30,
                cancellationToken);

        var auditItems = await _auditRead.QueryAsync(new UserAuditLogQuery
        {
            ActorUserId = targetUserId,
            FromUtc = from30,
            ToUtc = now,
            Take = 50,
        }, cancellationToken);

        var timeline = new List<EmployeePerformanceTimelineItemDto>();

        foreach (var op in ops30.OrderByDescending(o => o.CreatedAtUtc).Take(25))
        {
            timeline.Add(new EmployeePerformanceTimelineItemDto
            {
                OccurredAtUtc = op.CreatedAtUtc ?? now,
                Kind = "operation",
                TitleAr = $"عملية {op.Kind}",
                Subtitle = op.Status.ToString(),
                ReferenceId = op.Number,
                ActionUrl = $"/Telecom/TelecomHub?operationId={op.Id}",
            });
        }

        foreach (var t in tickets30.OrderByDescending(t => t.ResolvedAtUtc).Take(15))
        {
            timeline.Add(new EmployeePerformanceTimelineItemDto
            {
                OccurredAtUtc = t.ResolvedAtUtc ?? now,
                Kind = "ticket",
                TitleAr = "إغلاق تذكرة فنية",
                Subtitle = t.TicketNumber,
                ReferenceId = t.TicketNumber,
                ActionUrl = $"/Telecom/TechnicalTicketList?ticketId={t.Id}",
            });
        }

        foreach (var a in auditItems.Items)
        {
            timeline.Add(new EmployeePerformanceTimelineItemDto
            {
                OccurredAtUtc = a.OccurredAtUtc,
                Kind = "audit",
                TitleAr = a.SummaryAr ?? a.ActionType,
                Subtitle = a.ActionType,
                ReferenceId = a.Id,
            });
        }

        var orderedTimeline = timeline
            .OrderByDescending(t => t.OccurredAtUtc)
            .Take(50)
            .ToList();

        var created7 = ops30.Count(o => o.CreatedAtUtc >= from7);
        var created30 = ops30.Count;
        var resolved7 = tickets30.Count(t => t.ResolvedAtUtc >= from7);
        var resolved30 = tickets30.Count;

        var productivity = Math.Min(100m, Math.Round(
            created30 * 2m + resolved30 * 4m + payments30 * 2m, 2));

        return new GetEmployeePerformanceDetailResult
        {
            UserId = employee.UserId,
            DisplayName = employee.DisplayName,
            OrgUnitName = employee.OrgUnitNameAr,
            Roles = employee.Roles,
            IsOnline = employee.IsOnline,
            LastActivityUtc = employee.LastActivityUtc,
            Kpis = new EmployeePerformanceKpiDto
            {
                OperationsCreated7d = created7,
                OperationsCreated30d = created30,
                TicketsResolved7d = resolved7,
                TicketsResolved30d = resolved30,
                PaymentsCompleted30d = payments30,
                ProductivityScore30d = productivity,
            },
            Timeline = orderedTimeline,
        };
    }
}
