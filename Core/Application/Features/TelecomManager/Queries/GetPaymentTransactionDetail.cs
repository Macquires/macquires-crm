using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class GetPaymentTransactionDetailResult
{
    public string Id { get; init; } = "";
    public string Number { get; init; } = "";
    public string? CorrelationId { get; init; }
    public PaymentTransactionType TransactionType { get; init; }
    public PaymentTransactionStatus Status { get; init; }
    public PaymentChannel PaymentChannel { get; init; }
    public decimal Amount { get; init; }
    public string? Msisdn { get; init; }
    public string? GatewayReference { get; init; }
    public string? ReceiptNumber { get; init; }
    public decimal? BalanceBefore { get; init; }
    public decimal? BalanceAfter { get; init; }
    public string? FailureReason { get; init; }
    public DateTime? ConfirmedAtUtc { get; init; }
}

public class GetPaymentTransactionDetailRequest : IRequest<GetPaymentTransactionDetailResult?>
{
    public string Id { get; init; } = "";
}

public class GetPaymentTransactionDetailValidator : AbstractValidator<GetPaymentTransactionDetailRequest>
{
    public GetPaymentTransactionDetailValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class GetPaymentTransactionDetailHandler : IRequestHandler<GetPaymentTransactionDetailRequest, GetPaymentTransactionDetailResult?>
{
    private readonly IQueryContext _query;

    public GetPaymentTransactionDetailHandler(IQueryContext query) => _query = query;

    public async Task<GetPaymentTransactionDetailResult?> Handle(
        GetPaymentTransactionDetailRequest request,
        CancellationToken cancellationToken)
    {
        var row = await _query.TelecomPaymentTransaction.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (row is null)
        {
            return null;
        }

        return Map(row);
    }

    internal static GetPaymentTransactionDetailResult Map(TelecomPaymentTransaction row) =>
        new()
        {
            Id = row.Id,
            Number = row.Number,
            CorrelationId = row.CorrelationId,
            TransactionType = row.TransactionType,
            Status = row.Status,
            PaymentChannel = row.PaymentChannel,
            Amount = row.Amount,
            Msisdn = row.Msisdn,
            GatewayReference = row.GatewayReference,
            ReceiptNumber = row.ReceiptNumber,
            BalanceBefore = row.BalanceBefore,
            BalanceAfter = row.BalanceAfter,
            FailureReason = row.FailureReason,
            ConfirmedAtUtc = row.ConfirmedAtUtc,
        };
}
