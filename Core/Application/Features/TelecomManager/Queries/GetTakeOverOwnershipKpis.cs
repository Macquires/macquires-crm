using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record TakeOverReasonCountDto(string Reason, int Count);

public class GetTakeOverOwnershipKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<TakeOverReasonCountDto> TopReasons { get; init; } = new();
}

public class GetTakeOverOwnershipKpisRequest : IRequest<GetTakeOverOwnershipKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetTakeOverOwnershipKpisHandler : IRequestHandler<GetTakeOverOwnershipKpisRequest, GetTakeOverOwnershipKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(30);

    private readonly IQueryContext _context;

    public GetTakeOverOwnershipKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetTakeOverOwnershipKpisResult> Handle(
        GetTakeOverOwnershipKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.TakeOver
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.TransferReason,
                o.CreatedAtUtc,
                o.ConfirmedAtUtc,
                o.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var total = ops.Count;
        var completed = ops.Count(o => o.Status == TelecomOperationStatus.Completed);
        var failed = ops.Count(o => o.Status == TelecomOperationStatus.Failed);
        var pending = ops.Count(o => o.Status == TelecomOperationStatus.PendingDocuments);
        var terminal = completed + failed;
        var failRate = terminal > 0 ? Math.Round((decimal)failed / terminal * 100m, 2) : 0m;

        var slaSamples = ops
            .Where(o => o.Status == TelecomOperationStatus.Completed
                        && o.ConfirmedAtUtc != null
                        && o.CreatedAtUtc != null)
            .Select(o => o.ConfirmedAtUtc!.Value - o.CreatedAtUtc!.Value)
            .ToList();

        var slaPct = slaSamples.Count == 0
            ? 100m
            : Math.Round(
                (decimal)slaSamples.Count(d => d <= SlaTarget) / slaSamples.Count * 100m,
                2);

        var reasons = ops
            .Where(o => !string.IsNullOrWhiteSpace(o.TransferReason))
            .GroupBy(o => o.TransferReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new TakeOverReasonCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetTakeOverOwnershipKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            TopReasons = reasons,
        };
    }
}
