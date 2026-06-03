using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record ReconnectReasonCountDto(string Reason, int Count, int BackOfficeCount);

public class GetReconnectKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public int PaymentClearedToday { get; init; }
    public int FraudClearanceToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<ReconnectReasonCountDto> TopReasons { get; init; } = new();
}

public class GetReconnectKpisRequest : IRequest<GetReconnectKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetReconnectKpisHandler : IRequestHandler<GetReconnectKpisRequest, GetReconnectKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(30);

    private readonly IQueryContext _context;

    public GetReconnectKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetReconnectKpisResult> Handle(
        GetReconnectKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.Reconnect
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.ReconnectReason,
                o.ClearanceType,
                o.PaymentReference,
                o.FraudClearanceConfirmed,
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
        var paymentCleared = ops.Count(o =>
            !string.IsNullOrWhiteSpace(o.PaymentReference)
            || string.Equals(o.ClearanceType, "Payment", StringComparison.OrdinalIgnoreCase));
        var fraudClear = ops.Count(o => o.FraudClearanceConfirmed);
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
            .Where(o => !string.IsNullOrWhiteSpace(o.ReconnectReason))
            .GroupBy(o => o.ReconnectReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ReconnectReasonCountDto(
                g.Key,
                g.Count(),
                g.Count(x => string.Equals(x.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetReconnectKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            PaymentClearedToday = paymentCleared,
            FraudClearanceToday = fraudClear,
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            TopReasons = reasons,
        };
    }
}
