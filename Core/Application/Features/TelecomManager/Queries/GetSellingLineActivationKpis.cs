using Application.Common.CQS.Queries;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;
public record SellingLineRejectionReasonDto(string Reason, int Count);

public class GetSellingLineActivationKpisResult
{
    public int TotalVolume { get; init; }
    public int CompletedCount { get; init; }
    public int FailedCount { get; init; }
    public int PendingExternalCount { get; init; }
    public decimal CompletionRatePercent { get; init; }
    public decimal FalloutRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public decimal AvgHandlingTimeMinutes { get; init; }
    public int ManualOverrideCount { get; init; }
    public List<SellingLineRejectionReasonDto> RejectionReasons { get; init; } = new();
}

public class GetSellingLineActivationKpisRequest : IRequest<GetSellingLineActivationKpisResult>, IOperationalKpiRequest
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? RegionId { get; init; }
    public string? BranchId { get; init; }
}
public class GetSellingLineActivationKpisHandler
    : IRequestHandler<GetSellingLineActivationKpisRequest, GetSellingLineActivationKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(5);

    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;

    public GetSellingLineActivationKpisHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }
    public async Task<GetSellingLineActivationKpisResult> Handle(
        GetSellingLineActivationKpisRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId, request.BranchId, cancellationToken);
        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new GetSellingLineActivationKpisResult();
        }

        var from = request.FromUtc ?? DateTime.UtcNow.AddDays(-30);
        var to = request.ToUtc ?? DateTime.UtcNow;

        var query = _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(scope.EffectiveBranchIds)
            .Where(o => !o.IsDeleted                        && o.Kind == TelecomOperationKind.NewActivation
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to);

        var ops = await query
            .Select(o => new
            {
                o.Status,
                o.CreatedAtUtc,
                o.ConfirmedAtUtc,
                o.OverrideReasonCode
            })
            .ToListAsync(cancellationToken);

        var total = ops.Count;
        var completed = ops.Count(o => o.Status == TelecomOperationStatus.Completed);
        var failed = ops.Count(o => o.Status == TelecomOperationStatus.Failed);
        var pendingExt = ops.Count(o => o.Status == TelecomOperationStatus.PendingExternal);
        var terminal = completed + failed;
        var completionRate = terminal > 0 ? Math.Round((decimal)completed / terminal * 100m, 2) : 0m;
        var falloutRate = total > 0 ? Math.Round((decimal)failed / total * 100m, 2) : 0m;

        var completedOps = await query
            .Where(o => o.Status == TelecomOperationStatus.Completed
                        && o.ConfirmedAtUtc != null
                        && o.CreatedAtUtc != null)
            .Select(o => new { Created = o.CreatedAtUtc!.Value, Confirmed = o.ConfirmedAtUtc!.Value })
            .ToListAsync(cancellationToken);

        var slaHits = completedOps.Count(o => o.Confirmed - o.Created <= SlaTarget);
        var slaPercent = completedOps.Count > 0
            ? Math.Round((decimal)slaHits / completedOps.Count * 100m, 2)
            : 0m;

        var avgMinutes = completedOps.Count > 0
            ? Math.Round((decimal)completedOps.Average(o => (o.Confirmed - o.Created).TotalMinutes), 2)
            : 0m;

        var manualOverrides = ops.Count(o => !string.IsNullOrWhiteSpace(o.OverrideReasonCode));

        var rejectionNotes = await _context.TelecomOperationAuditLog.AsNoTracking()
            .Where(a => !a.IsDeleted
                        && a.ToStatus == TelecomOperationStatus.Failed
                        && a.OccurredAtUtc >= from
                        && a.OccurredAtUtc <= to
                        && a.BranchId != null
                        && scope.EffectiveBranchIds.Contains(a.BranchId))            .Join(
                _context.TelecomOperationRequest.AsNoTracking().Where(o =>
                    !o.IsDeleted && o.Kind == TelecomOperationKind.NewActivation),
                a => a.TelecomOperationRequestId,
                o => o.Id,
                (a, o) => a.Note)
            .Where(n => n != null)
            .ToListAsync(cancellationToken);

        var rejectionRows = rejectionNotes
            .GroupBy(n => n!)
            .Select(g => new SellingLineRejectionReasonDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        return new GetSellingLineActivationKpisResult
        {
            TotalVolume = total,
            CompletedCount = completed,
            FailedCount = failed,
            PendingExternalCount = pendingExt,
            CompletionRatePercent = completionRate,
            FalloutRatePercent = falloutRate,
            SlaCompliancePercent = slaPercent,
            AvgHandlingTimeMinutes = avgMinutes,
            ManualOverrideCount = manualOverrides,
            RejectionReasons = rejectionRows
        };
    }
}
