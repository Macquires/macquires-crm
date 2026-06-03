using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom.PaymentServices;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class PaymentTransactionListItemDto
{
    public string Id { get; init; } = "";
    public string Number { get; init; } = "";
    public string? Msisdn { get; init; }
    public PaymentTransactionType TransactionType { get; init; }
    public PaymentTransactionStatus Status { get; init; }
    public decimal Amount { get; init; }
    public string? GatewayReference { get; init; }
    public string? ReceiptNumber { get; init; }
    public DateTime? ConfirmedAtUtc { get; init; }
    public bool CanReverse { get; init; }
}

public class GetPaymentTransactionListResult
{
    public List<PaymentTransactionListItemDto> Items { get; init; } = new();
}

public class GetPaymentTransactionListRequest : IRequest<GetPaymentTransactionListResult>
{
    public int Take { get; init; } = 30;
}

public class GetPaymentTransactionListHandler : IRequestHandler<GetPaymentTransactionListRequest, GetPaymentTransactionListResult>
{
    private readonly IQueryContext _query;

    public GetPaymentTransactionListHandler(IQueryContext query) => _query = query;

    public async Task<GetPaymentTransactionListResult> Handle(
        GetPaymentTransactionListRequest request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var now = DateTime.UtcNow;
        var window = TimeSpan.FromMinutes(PaymentServicesConstants.ReversalWindowMinutes);

        var rows = await _query.TelecomPaymentTransaction.AsNoTracking().IsDeletedEqualTo()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.Number,
                p.Msisdn,
                p.TransactionType,
                p.Status,
                p.Amount,
                p.GatewayReference,
                p.ReceiptNumber,
                p.ConfirmedAtUtc,
                p.ReversedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(p =>
        {
            var canReverse = p.Status == PaymentTransactionStatus.Completed
                && !p.ReversedAtUtc.HasValue
                && p.ConfirmedAtUtc.HasValue
                && now - p.ConfirmedAtUtc.Value <= window;

            return new PaymentTransactionListItemDto
            {
                Id = p.Id,
                Number = p.Number,
                Msisdn = p.Msisdn,
                TransactionType = p.TransactionType,
                Status = p.Status,
                Amount = p.Amount,
                GatewayReference = p.GatewayReference,
                ReceiptNumber = p.ReceiptNumber,
                ConfirmedAtUtc = p.ConfirmedAtUtc,
                CanReverse = canReverse,
            };
        }).ToList();

        return new GetPaymentTransactionListResult { Items = items };
    }
}
