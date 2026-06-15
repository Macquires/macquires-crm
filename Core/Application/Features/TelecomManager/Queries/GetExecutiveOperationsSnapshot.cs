using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Application.Common.Telecom.PaymentServices;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public sealed class ExecutivePendingOperationRowDto
{
    public string OperationId { get; init; } = null!;
    public string OperationNumber { get; init; } = null!;
    public TelecomOperationKind Kind { get; init; }
    public TelecomOperationStatus Status { get; init; }
    public string? BranchName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public string? ActionUrl { get; init; }
}

public sealed class ExecutiveOverdueTicketRowDto
{
    public string TicketId { get; init; } = null!;
    public string TicketNumber { get; init; } = null!;
    public TechnicalTicketPriority Priority { get; init; }
    public string? BranchName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public double OverdueHours { get; init; }
    public string? ActionUrl { get; init; }
}

public class GetExecutiveOperationsSnapshotResult
{
    public decimal PaymentRechargeToday { get; init; }
    public int PaymentCompletedToday { get; init; }
    public decimal PaymentFailureRatePercent { get; init; }
    public int ActivationsToday { get; init; }
    public int PendingOperations { get; init; }
    public int OverdueTickets { get; init; }
    public decimal PaymentSlaPercent { get; init; }
    public IReadOnlyList<ExecutivePendingOperationRowDto> TopPendingOperations { get; init; } = [];
    public IReadOnlyList<ExecutiveOverdueTicketRowDto> TopOverdueTickets { get; init; } = [];
    public string ScopeLabelAr { get; init; } = null!;
}

public record GetExecutiveOperationsSnapshotRequest(
    string? RegionId,
    string? BranchId,
    DateTime? FromUtc,
    DateTime? ToUtc)
    : IRequest<GetExecutiveOperationsSnapshotResult>, IOperationalKpiRequest;

public class GetExecutiveOperationsSnapshotHandler
    : IRequestHandler<GetExecutiveOperationsSnapshotRequest, GetExecutiveOperationsSnapshotResult>
{
    private static readonly TimeSpan SlaCritical = TimeSpan.FromHours(4);
    private static readonly TimeSpan SlaHigh = TimeSpan.FromHours(8);
    private static readonly TimeSpan SlaNormal = TimeSpan.FromHours(24);

    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IMediator _mediator;

    public GetExecutiveOperationsSnapshotHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService,
        IMediator mediator)
    {
        _context = context;
        _scopeService = scopeService;
        _mediator = mediator;
    }

    public async Task<GetExecutiveOperationsSnapshotResult> Handle(
        GetExecutiveOperationsSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new GetExecutiveOperationsSnapshotResult { ScopeLabelAr = scope.ScopeLabelAr };
        }

        var branchIds = scope.EffectiveBranchIds;
        var now = DateTime.UtcNow;
        var from = request.FromUtc ?? now.Date;
        var to = request.ToUtc ?? now;

        var paymentKpis = await _mediator.Send(
            new GetPaymentServicesKpisRequest
            {
                RegionId = request.RegionId,
                BranchId = request.BranchId,
                FromUtc = from,
                ToUtc = to,
            },
            cancellationToken);

        var activationKpis = await _mediator.Send(
            new GetSellingLineActivationKpisRequest
            {
                RegionId = request.RegionId,
                BranchId = request.BranchId,
                FromUtc = from,
                ToUtc = to,
            },
            cancellationToken);

        var branchNames = await _context.OrgUnit.AsNoTracking()
            .Where(o => branchIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.NameAr, cancellationToken);

        var pendingStatuses = new[]
        {
            TelecomOperationStatus.Draft,
            TelecomOperationStatus.Confirmed,
            TelecomOperationStatus.PendingExternal,
            TelecomOperationStatus.PendingDocuments,
            TelecomOperationStatus.Provisioning,
            TelecomOperationStatus.ProvisioningError,
            TelecomOperationStatus.Approved_Pending_Cash,
            TelecomOperationStatus.Paid_Pending_BackOffice_Clearance,
            TelecomOperationStatus.In_Progress,
            TelecomOperationStatus.Scheduled,
        };

        var pendingOps = await _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(branchIds)
            .Where(o => !o.IsDeleted && pendingStatuses.Contains(o.Status))
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(10)
            .Select(o => new
            {
                o.Id,
                o.Number,
                o.Kind,
                o.Status,
                o.BranchId,
                o.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var customerIdsInScope = await _context.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.OrgUnitId != null && branchIds.Contains(c.OrgUnitId))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var openTickets = customerIdsInScope.Count == 0
            ? []
            : await _context.TelecomTechnicalTicket.AsNoTracking()
                .Where(t => !t.IsDeleted
                    && t.CustomerId != null
                    && customerIdsInScope.Contains(t.CustomerId)
                    && t.Status != TechnicalTicketStatus.Resolved
                    && t.CreatedAtUtc.HasValue)
                .Select(t => new
                {
                    t.Id,
                    t.TicketNumber,
                    t.Priority,
                    t.BranchId,
                    t.CreatedAtUtc,
                })
                .ToListAsync(cancellationToken);

        var overdue = openTickets
            .Select(t =>
            {
                var target = t.Priority switch
                {
                    TechnicalTicketPriority.Critical => SlaCritical,
                    TechnicalTicketPriority.High => SlaHigh,
                    _ => SlaNormal,
                };
                var elapsed = now - t.CreatedAtUtc!.Value;
                var overdueHours = (elapsed - target).TotalHours;
                return new { Ticket = t, OverdueHours = overdueHours };
            })
            .Where(x => x.OverdueHours > 0)
            .OrderByDescending(x => x.OverdueHours)
            .Take(10)
            .ToList();

        var pendingCount = await _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(branchIds)
            .CountAsync(o => !o.IsDeleted && pendingStatuses.Contains(o.Status), cancellationToken);

        return new GetExecutiveOperationsSnapshotResult
        {
            PaymentRechargeToday = paymentKpis.TotalRechargedAmountToday,
            PaymentCompletedToday = paymentKpis.CompletedCountToday,
            PaymentFailureRatePercent = paymentKpis.FailureRatePercent,
            PaymentSlaPercent = paymentKpis.SlaCompliancePercent,
            ActivationsToday = activationKpis.CompletedCount,
            PendingOperations = pendingCount,
            OverdueTickets = overdue.Count,
            TopPendingOperations = pendingOps.Select(o => new ExecutivePendingOperationRowDto
            {
                OperationId = o.Id,
                OperationNumber = o.Number,
                Kind = o.Kind,
                Status = o.Status,
                BranchName = o.BranchId != null && branchNames.TryGetValue(o.BranchId, out var bn) ? bn : null,
                CreatedAtUtc = o.CreatedAtUtc,
                ActionUrl = $"/Telecom/TelecomHub?operationId={o.Id}",
            }).ToList(),
            TopOverdueTickets = overdue.Select(x => new ExecutiveOverdueTicketRowDto
            {
                TicketId = x.Ticket.Id,
                TicketNumber = x.Ticket.TicketNumber,
                Priority = x.Ticket.Priority,
                BranchName = x.Ticket.BranchId != null && branchNames.TryGetValue(x.Ticket.BranchId, out var bn)
                    ? bn
                    : null,
                CreatedAtUtc = x.Ticket.CreatedAtUtc,
                OverdueHours = Math.Round(x.OverdueHours, 1),
                ActionUrl = $"/Telecom/TechnicalTicketList?ticketId={x.Ticket.Id}",
            }).ToList(),
            ScopeLabelAr = scope.ScopeLabelAr,
        };
    }
}
