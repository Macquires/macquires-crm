using Application.Common.CQS.Queries;
using Application.Common.Telecom.Analytics;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class GetBadDebtKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public decimal CollectedAmountToday { get; init; }
    public decimal WriteOffAmountToday { get; init; }
    public int PaymentPlansToday { get; init; }
    public decimal FailureRatePercent { get; init; }
}

public class GetBadDebtKpisRequest : IRequest<GetBadDebtKpisResult>, IOperationalKpiRequest
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? RegionId { get; init; }
    public string? BranchId { get; init; }
}

public class GetBadDebtKpisHandler : IRequestHandler<GetBadDebtKpisRequest, GetBadDebtKpisResult>
{
    private readonly IQueryContext _context;
    private readonly IOperationalAnalyticsScopeService _scopeService;

    public GetBadDebtKpisHandler(
        IQueryContext context,
        IOperationalAnalyticsScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public async Task<GetBadDebtKpisResult> Handle(
        GetBadDebtKpisRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId, request.BranchId, cancellationToken);
        if (scope.EffectiveBranchIds.Count == 0)
        {
            return new GetBadDebtKpisResult();
        }

        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .InBranchScope(scope.EffectiveBranchIds)
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.BadDebtRecovery
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.CollectionAction,
                o.CollectedAmount,
                o.WriteOffAmount,
                o.ApprovalLevelRequired,
            })
            .ToListAsync(cancellationToken);

        var total = ops.Count;
        var completed = ops.Count(o => o.Status == TelecomOperationStatus.Completed);
        var failed = ops.Count(o => o.Status == TelecomOperationStatus.Failed);
        var pending = ops.Count(o =>
            o.Status == TelecomOperationStatus.PendingDocuments
            && string.Equals(o.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal));
        var collected = ops
            .Where(o => o.Status == TelecomOperationStatus.Completed
                        && string.Equals(o.CollectionAction, "PaymentRecorded", StringComparison.OrdinalIgnoreCase))
            .Sum(o => o.CollectedAmount ?? 0m);
        var writeOff = ops
            .Where(o => o.Status == TelecomOperationStatus.Completed
                        && (string.Equals(o.CollectionAction, "WriteOffPartial", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(o.CollectionAction, "WriteOffFull", StringComparison.OrdinalIgnoreCase)))
            .Sum(o => o.WriteOffAmount ?? 0m);
        var plans = ops.Count(o =>
            string.Equals(o.CollectionAction, "PaymentPlan", StringComparison.OrdinalIgnoreCase));
        var terminal = completed + failed;
        var failRate = terminal > 0 ? Math.Round((decimal)failed / terminal * 100m, 2) : 0m;

        return new GetBadDebtKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            CollectedAmountToday = collected,
            WriteOffAmountToday = writeOff,
            PaymentPlansToday = plans,
            FailureRatePercent = failRate,
        };
    }
}
