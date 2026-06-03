using Application.Common.CQS.Queries;
using Application.Common.Telecom.Refund;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record RefundRejectionDto(string Reason, int Count);

public class GetRefundKpisResult
{
    public int TotalToday { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public int PendingBackOffice { get; init; }
    public decimal SettledAmountToday { get; init; }
    public int DualApprovalToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public List<RefundRejectionDto> RejectionReasons { get; init; } = new();
}

public class GetRefundKpisRequest : IRequest<GetRefundKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetRefundKpisHandler : IRequestHandler<GetRefundKpisRequest, GetRefundKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(45);

    private readonly IQueryContext _context;

    public GetRefundKpisHandler(IQueryContext context) => _context = context;

    public async Task<GetRefundKpisResult> Handle(
        GetRefundKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var ops = await _context.TelecomOperationRequest.AsNoTracking()
            .Where(o => !o.IsDeleted
                        && o.Kind == TelecomOperationKind.DepositRefundSettlement
                        && o.CreatedAtUtc >= from
                        && o.CreatedAtUtc <= to)
            .Select(o => new
            {
                o.Status,
                o.RefundType,
                o.RefundReason,
                o.RefundAmount,
                o.RefundSettlementStatus,
                o.ApprovalLevelRequired,
                o.RequiresDualApproval,
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
        var settledAmount = ops
            .Where(o => o.Status == TelecomOperationStatus.Completed
                        && string.Equals(o.RefundSettlementStatus, RefundWellKnown.SettlementSettled, StringComparison.Ordinal))
            .Sum(o => o.RefundAmount ?? 0m);
        var dual = ops.Count(o => o.RequiresDualApproval);
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

        var rejections = ops
            .Where(o => o.Status == TelecomOperationStatus.Failed && !string.IsNullOrWhiteSpace(o.RefundReason))
            .GroupBy(o => o.RefundReason!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new RefundRejectionDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new GetRefundKpisResult
        {
            TotalToday = total,
            CompletedToday = completed,
            FailedToday = failed,
            PendingBackOffice = pending,
            SettledAmountToday = settledAmount,
            DualApprovalToday = dual,
            FailureRatePercent = failRate,
            SlaCompliancePercent = slaPct,
            RejectionReasons = rejections,
        };
    }
}
