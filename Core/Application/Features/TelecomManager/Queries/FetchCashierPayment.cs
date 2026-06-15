using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Security;
using Application.Common.Telecom.SellingLine;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public class FetchCashierPaymentResult
{
    public CashierPaymentResultDto? Data { get; init; }
}

public class FetchCashierPaymentRequest : IRequest<FetchCashierPaymentResult>, IRequireAnyPermission
{
    public string PaymentReference { get; init; } = null!;
    public string? OperationId { get; init; }
    public decimal? ExpectedAmount { get; init; }

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.PaymentAny;
}

public class FetchCashierPaymentValidator : AbstractValidator<FetchCashierPaymentRequest>
{
    public FetchCashierPaymentValidator()
    {
        RuleFor(x => x.PaymentReference).NotEmpty().MaximumLength(128);
    }
}

public class FetchCashierPaymentHandler : IRequestHandler<FetchCashierPaymentRequest, FetchCashierPaymentResult>
{
    private readonly IPosCashierIntegration _cashier;
    private readonly IQueryContext _query;

    public FetchCashierPaymentHandler(IPosCashierIntegration cashier, IQueryContext query)
    {
        _cashier = cashier;
        _query = query;
    }

    public async Task<FetchCashierPaymentResult> Handle(
        FetchCashierPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var expected = await ResolveExpectedAmountAsync(request, cancellationToken);
        if (expected <= 0 && request.ExpectedAmount is not > 0 && string.IsNullOrEmpty(request.OperationId))
        {
            throw new BusinessRuleViolationException(
                "حدد مبلغ الوديعة المتوقع أو اربط العملية قبل جلب بيانات الكاشير.");
        }

        var lookupAmount = expected > 0 ? expected : request.ExpectedAmount ?? 0m;
        var result = await _cashier.FetchPaymentByReferenceAsync(
            request.PaymentReference.Trim(),
            lookupAmount,
            cancellationToken);

        if (!result.Success)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        return new FetchCashierPaymentResult { Data = result };
    }

    private async Task<decimal> ResolveExpectedAmountAsync(
        FetchCashierPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExpectedAmount is > 0)
        {
            return request.ExpectedAmount.Value;
        }

        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            return 0m;
        }

        var op = await _query.TelecomOperationRequest.AsNoTracking()
            .FirstOrDefaultAsync(
                o => !o.IsDeleted && o.Id == request.OperationId && o.Kind == TelecomOperationKind.NewActivation,
                cancellationToken)
            ?? throw new BusinessRuleViolationException("عملية التفعيل غير موجودة.");

        return await SellingLineDepositResolver.ResolveRequiredDepositAsync(_query, op, cancellationToken);
    }
}
