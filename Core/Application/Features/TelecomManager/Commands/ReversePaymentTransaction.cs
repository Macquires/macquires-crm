using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.PaymentServices;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class ReversePaymentTransactionResult
{
    public bool Success { get; init; }
    public string MessageAr { get; init; } = "";
    public decimal? NewBalance { get; init; }
    public string? PaymentNumber { get; init; }
}

public class ReversePaymentTransactionRequest : IRequest<ReversePaymentTransactionResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.FinanceBdrExecute;
    public string PaymentId { get; init; } = "";
    public string ReasonCode { get; init; } = "";
}

public class ReversePaymentTransactionValidator : AbstractValidator<ReversePaymentTransactionRequest>
{
    public ReversePaymentTransactionValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
    }
}

public class ReversePaymentTransactionHandler : IRequestHandler<ReversePaymentTransactionRequest, ReversePaymentTransactionResult>
{
    private readonly IPaymentServicesReversalService _reversal;
    private readonly IOperatorContext _operatorContext;

    public ReversePaymentTransactionHandler(
        IPaymentServicesReversalService reversal,
        IOperatorContext operatorContext)
    {
        _reversal = reversal;
        _operatorContext = operatorContext;
    }

    public async Task<ReversePaymentTransactionResult> Handle(
        ReversePaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (!PaymentServicesConstants.RolesAllowedReversal.Any(
                r => _operatorContext.Roles.Contains(r, StringComparer.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleViolationException("صلاحية عكس الدفع متاحة لقسم المالية/الإدارة فقط.");
        }

        var result = await _reversal.ReverseAsync(
            request.PaymentId,
            request.ReasonCode,
            OperatorActor.RequireUserId(_operatorContext),
            cancellationToken);

        return new ReversePaymentTransactionResult
        {
            Success = result.Success,
            MessageAr = result.MessageAr,
            NewBalance = result.NewBalance,
            PaymentNumber = result.PaymentNumber,
        };
    }
}
