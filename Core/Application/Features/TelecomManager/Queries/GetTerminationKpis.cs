using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record TerminationReasonCountDto(string Reason, int Count, int BackOfficeCount);

public class GetTerminationKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public int VoluntaryToday { get; init; }
    public decimal FinalBillTotalToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<TerminationReasonCountDto> TopReasons { get; init; } = new();
}

public class GetTerminationKpisRequest : IRequest<GetTerminationKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetTerminationKpisHandler : IRequestHandler<GetTerminationKpisRequest, GetTerminationKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(30);

    private readonly IQueryContext _context;

    public GetTerminationKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetTerminationKpisResult> Handle(
        GetTerminationKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.Termination
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.TerminationType,
                o.TerminationReason,
                o.FinalBillAmount,
                o.ApprovalLevelRequired,
                o.CreatedAtUtc,
                o.ConfirmedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var total = ops.Count;
        var completed = ops.Count(o => o.Status == TelecomOperationStatus.Completed);
        var failed = ops.Count(o => o.Status == TelecomOperationStatus.Failed);
        var pending = ops.Count(o =>
            o.Status == TelecomOperationStatus.PendingDocuments
            && string.Equals(o.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal));
        var voluntary = ops.Count(o =>
            string.Equals(o.TerminationType, "Voluntary", StringComparison.OrdinalIgnoreCase));
        var finalBills = ops.Where(o => o.FinalBillAmount is > 0).Sum(o => o.FinalBillAmount!.Value);
        var terminal = completed + failed;
        var failRate = terminal > 0 ? Math.Round((decimal)failed / terminal * 100m, 2) : 0m;

        var slaSamples = ops
            .Where(o => o.Status == TelecomOperationStatus.Completed
                        && o.ConfirmedAtUtc.HasValue
                        && o.CreatedAtUtc.HasValue)
            .Select(o => o.ConfirmedAtUtc!.Value - o.CreatedAtUtc!.Value)
            .ToList();

        var slaPct = slaSamples.Count == 0
            ? 100m
            : Math.Round(
                (decimal)slaSamples.Count(d => d <= SlaTarget) / slaSamples.Count * 100m,
                2);

        var reasons = ops
            .Where(o => !string.IsNullOrWhiteSpace(o.TerminationReason))
            .GroupBy(o => o.TerminationReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new TerminationReasonCountDto(
                g.Key,
                g.Count(),
                g.Count(x => string.Equals(x.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetTerminationKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            VoluntaryToday = voluntary,
            FinalBillTotalToday = finalBills,
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            TopReasons = reasons,
        };
    }
}
