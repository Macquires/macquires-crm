using Application.Common.CQS.Queries;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record SimSwapReasonCountDto(string Reason, int Count, int LostOrStolenCount);

public class GetSimSwapKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public int LostOrStolenToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<SimSwapReasonCountDto> TopReasons { get; init; } = new();
}

public class GetSimSwapKpisRequest : IRequest<GetSimSwapKpisResult>, IOperationalKpiRequest
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? RegionId { get; init; }
    public string? BranchId { get; init; }
}

public class GetSimSwapKpisHandler : IRequestHandler<GetSimSwapKpisRequest, GetSimSwapKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(20);

    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;

    public GetSimSwapKpisHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public async Task<GetSimSwapKpisResult> Handle(
        GetSimSwapKpisRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId, request.BranchId, cancellationToken);
        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new GetSimSwapKpisResult();
        }

        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(scope.EffectiveBranchIds)
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.SimSwap
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.ReplacementReason,
                o.IsLostOrStolenReport,
                o.CreatedAtUtc,
                o.ConfirmedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var total = ops.Count;
        var completed = ops.Count(o => o.Status == TelecomOperationStatus.Completed);
        var failed = ops.Count(o => o.Status == TelecomOperationStatus.Failed);
        var pending = ops.Count(o => o.Status == TelecomOperationStatus.PendingDocuments);
        var lostStolen = ops.Count(o => o.IsLostOrStolenReport);
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
            .Where(o => !string.IsNullOrWhiteSpace(o.ReplacementReason))
            .GroupBy(o => o.ReplacementReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SimSwapReasonCountDto(
                g.Key,
                g.Count(),
                g.Count(x => x.IsLostOrStolenReport)))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetSimSwapKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            LostOrStolenToday = lostStolen,
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            TopReasons = reasons,
        };
    }
}
