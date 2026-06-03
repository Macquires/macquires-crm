using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record SuspensionReasonCountDto(string Reason, int Count, int BackOfficeCount);

public class GetSuspensionKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public int FraudToday { get; init; }
    public int AutoReconnectEnabledToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<SuspensionReasonCountDto> TopReasons { get; init; } = new();
}

public class GetSuspensionKpisRequest : IRequest<GetSuspensionKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetSuspensionKpisHandler : IRequestHandler<GetSuspensionKpisRequest, GetSuspensionKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(30);

    private readonly IQueryContext _context;

    public GetSuspensionKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetSuspensionKpisResult> Handle(
        GetSuspensionKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.TemporarySuspension
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.SuspensionType,
                o.SuspensionReason,
                o.ApprovalLevelRequired,
                o.AutoReconnectEnabled,
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
        var fraud = ops.Count(o =>
            string.Equals(o.SuspensionType, "Fraud", StringComparison.OrdinalIgnoreCase));
        var autoReconnect = ops.Count(o => o.AutoReconnectEnabled);
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
            .Where(o => !string.IsNullOrWhiteSpace(o.SuspensionReason))
            .GroupBy(o => o.SuspensionReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SuspensionReasonCountDto(
                g.Key,
                g.Count(),
                g.Count(x => string.Equals(x.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetSuspensionKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            FraudToday = fraud,
            AutoReconnectEnabledToday = autoReconnect,
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            TopReasons = reasons,
        };
    }
}
