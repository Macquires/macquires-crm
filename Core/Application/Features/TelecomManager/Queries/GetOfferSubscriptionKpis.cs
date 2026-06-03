using Application.Common.CQS.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record TopOfferingCountDto(string Label, int Count);

public class GetOfferSubscriptionKpisResult
{
    public int MigrationTotalToday { get; init; }
    public int MigrationCompletedToday { get; init; }
    public int MigrationFailedToday { get; init; }
    public int VasActivateToday { get; init; }
    public int VasDeactivateToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<TopOfferingCountDto> TopMigratedOffers { get; init; } = new();
}

public class GetOfferSubscriptionKpisRequest : IRequest<GetOfferSubscriptionKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetOfferSubscriptionKpisHandler : IRequestHandler<GetOfferSubscriptionKpisRequest, GetOfferSubscriptionKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(30);

    private readonly IQueryContext _context;

    public GetOfferSubscriptionKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetOfferSubscriptionKpisResult> Handle(
        GetOfferSubscriptionKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var mgr = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.Migration
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.TargetOfferName,
                o.ProductOfferingId,
                o.CreatedAtUtc,
                o.ConfirmedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var vas = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.ServiceModification
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new { o.Status, o.Notes })
            .ToListAsync(cancellationToken);

        var mgrCompleted = mgr.Count(o => o.Status == TelecomOperationStatus.Completed);
        var mgrFailed = mgr.Count(o => o.Status == TelecomOperationStatus.Failed);
        var mgrTerminal = mgrCompleted + mgrFailed;
        var failRate = mgrTerminal > 0 ? Math.Round((decimal)mgrFailed / mgrTerminal * 100m, 2) : 0m;

        var slaSamples = mgr
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

        var topOffers = mgr
            .Where(o => o.Status == TelecomOperationStatus.Completed)
            .Select(o => !string.IsNullOrWhiteSpace(o.TargetOfferName)
                ? o.TargetOfferName!.Trim()
                : (o.ProductOfferingId ?? "—"))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TopOfferingCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetOfferSubscriptionKpisResult
        {
            MigrationTotalToday = mgr.Count,
            MigrationCompletedToday = mgrCompleted,
            MigrationFailedToday = mgrFailed,
            VasActivateToday = vas.Count(v =>
                v.Notes != null && v.Notes.Contains("Activate VAS", StringComparison.OrdinalIgnoreCase)),
            VasDeactivateToday = vas.Count(v =>
                v.Notes != null && v.Notes.Contains("Deactivate VAS", StringComparison.OrdinalIgnoreCase)),
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            TopMigratedOffers = topOffers,
        };
    }
}
