using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record ChangeNumberReasonCountDto(string Reason, int Count, int PremiumCount);

public class GetChangeNumberKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public int PremiumToday { get; init; }
    public decimal PremiumFeeTotalToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<ChangeNumberReasonCountDto> TopReasons { get; init; } = new();
}

public class GetChangeNumberKpisRequest : IRequest<GetChangeNumberKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetChangeNumberKpisHandler : IRequestHandler<GetChangeNumberKpisRequest, GetChangeNumberKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(20);

    private readonly IQueryContext _context;

    public GetChangeNumberKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetChangeNumberKpisResult> Handle(
        GetChangeNumberKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.NumberPortability
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.NumberChangeReason,
                o.PremiumFeeAmount,
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
        var premium = ops.Count(o => o.PremiumFeeAmount is > 0
                                     || string.Equals(o.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal));
        var premiumFees = ops.Where(o => o.PremiumFeeAmount is > 0).Sum(o => o.PremiumFeeAmount!.Value);
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
            .Where(o => !string.IsNullOrWhiteSpace(o.NumberChangeReason))
            .GroupBy(o => o.NumberChangeReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ChangeNumberReasonCountDto(
                g.Key,
                g.Count(),
                g.Count(x => x.PremiumFeeAmount is > 0
                             || string.Equals(x.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetChangeNumberKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            PremiumToday = premium,
            PremiumFeeTotalToday = premiumFees,
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            TopReasons = reasons,
        };
    }
}
