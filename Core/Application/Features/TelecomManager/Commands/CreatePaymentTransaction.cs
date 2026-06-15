using Application.Common.Security;
using Application.Common.Telecom.PaymentServices;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class CreatePaymentTransactionResult
{
    public string PaymentId { get; init; } = "";
    public string Number { get; init; } = "";
    public string? CorrelationId { get; init; }
    public PaymentTransactionStatus Status { get; init; }
}

public class CreatePaymentTransactionRequest : IRequest<CreatePaymentTransactionResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.TelecomLineRecharge;
    public PaymentTransactionType Type { get; init; } = PaymentTransactionType.Recharge;
    public string CustomerId { get; init; } = "";
    public string SubscriptionId { get; init; } = "";
    public decimal Amount { get; init; }
    public PaymentChannel PaymentChannel { get; init; } = PaymentChannel.Wallet;
    public PaymentServiceChannel ServiceChannel { get; init; } = PaymentServiceChannel.Showroom;
    public string? VoucherCode { get; init; }
}

public class CreatePaymentTransactionValidator : AbstractValidator<CreatePaymentTransactionRequest>
{
    public CreatePaymentTransactionValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Type == PaymentTransactionType.Recharge);
        RuleFor(x => x.VoucherCode).NotEmpty().When(x => x.Type == PaymentTransactionType.VoucherRedeem);
    }
}

public class CreatePaymentTransactionHandler : IRequestHandler<CreatePaymentTransactionRequest, CreatePaymentTransactionResult>
{
    private readonly IPaymentServicesOrchestrator _orchestrator;
    private readonly IOperatorContext _operator;

    public CreatePaymentTransactionHandler(
        IPaymentServicesOrchestrator orchestrator,
        IOperatorContext operatorContext)
    {
        _orchestrator = orchestrator;
        _operator = operatorContext;
    }

    public async Task<CreatePaymentTransactionResult> Handle(
        CreatePaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);
        PaymentDraftResult draft;
        if (request.Type == PaymentTransactionType.VoucherRedeem)
        {
            if (string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                throw new InvalidOperationException("رمز القسيمة مطلوب.");
            }

            draft = await _orchestrator.CreateVoucherRedeemDraftAsync(
                request.CustomerId,
                request.SubscriptionId,
                request.VoucherCode,
                request.ServiceChannel,
                actorUserId,
                cancellationToken);
        }
        else if (request.Type == PaymentTransactionType.Recharge)
        {
            draft = await _orchestrator.CreateRechargeDraftAsync(
                request.CustomerId,
                request.SubscriptionId,
                request.Amount,
                request.PaymentChannel,
                request.ServiceChannel,
                actorUserId,
                cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("نوع المعاملة غير مدعوم.");
        }

        return new CreatePaymentTransactionResult
        {
            PaymentId = draft.PaymentId,
            Number = draft.Number,
            CorrelationId = draft.CorrelationId,
            Status = draft.Status,
        };
    }
}
