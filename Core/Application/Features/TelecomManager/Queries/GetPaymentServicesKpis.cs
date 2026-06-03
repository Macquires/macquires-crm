using Application.Common.CQS.Queries;
using Application.Common.Telecom.PaymentServices;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class GetPaymentServicesKpisResult
{
    public decimal TotalRechargedAmountToday { get; init; }
    public int CompletedCountToday { get; init; }
    public int FailedCountToday { get; init; }
    public decimal FailureRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public int ReversedCountToday { get; init; }
}

public class GetPaymentServicesKpisRequest : IRequest<GetPaymentServicesKpisResult>
{
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

public class GetPaymentServicesKpisHandler : IRequestHandler<GetPaymentServicesKpisRequest, GetPaymentServicesKpisResult>
{
    private static readonly TimeSpan SlaTarget = TimeSpan.FromMinutes(2);

    private readonly IQueryContext _query;

    public GetPaymentServicesKpisHandler(IQueryContext query) => _query = query;

    public async Task<GetPaymentServicesKpisResult> Handle(
        GetPaymentServicesKpisRequest request,
        CancellationToken cancellationToken)
    {
        var from = request.FromUtc ?? DateTime.UtcNow.Date;
        var to = request.ToUtc ?? DateTime.UtcNow;

        var rows = await _query.TelecomPaymentTransaction.AsNoTracking()
            .Where(p => !p.IsDeleted && p.CreatedAtUtc >= from && p.CreatedAtUtc <= to)
            .Select(p => new
            {
                p.Status,
                p.Amount,
                p.CreatedAtUtc,
                p.ConfirmedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var completed = rows.Where(p => p.Status == PaymentTransactionStatus.Completed).ToList();
        var failed = rows.Count(p => p.Status == PaymentTransactionStatus.Failed);
        var reversed = rows.Count(p => p.Status == PaymentTransactionStatus.Reversed);
        var terminal = completed.Count + failed;
        var failureRate = terminal > 0 ? Math.Round((decimal)failed / terminal * 100m, 2) : 0m;

        var slaHits = completed.Count(p =>
            p.ConfirmedAtUtc.HasValue
            && p.CreatedAtUtc.HasValue
            && p.ConfirmedAtUtc.Value - p.CreatedAtUtc.Value <= SlaTarget);

        var slaPct = completed.Count > 0
            ? Math.Round((decimal)slaHits / completed.Count * 100m, 2)
            : 0m;

        return new GetPaymentServicesKpisResult
        {
            TotalRechargedAmountToday = completed.Sum(p => p.Amount),
            CompletedCountToday = completed.Count,
            FailedCountToday = failed,
            FailureRatePercent = failureRate,
            SlaCompliancePercent = slaPct,
            ReversedCountToday = reversed,
        };
    }
}
