using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.PaymentServices;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerManager.Commands;

public class RechargeCustomer360LineResult
{
    public bool Success { get; init; }
    public string? Msisdn { get; init; }
    public decimal RechargedAmount { get; init; }
    public decimal NewBalance { get; init; }
    public string Currency { get; init; } = "SYP";
    public string? Message { get; init; }
    public string? PaymentId { get; init; }
    public string? PaymentNumber { get; init; }
    public string? ReceiptNumber { get; init; }
    public string? CorrelationId { get; init; }
}

public class RechargeCustomer360LineRequest : IRequest<RechargeCustomer360LineResult>
{
    public string CustomerId { get; init; } = "";
    public string SubscriptionId { get; init; } = "";
    public decimal Amount { get; init; }
    /// <summary>مرجع بوابة الدفع — إن تُرك فارغاً يُولَّد تلقائياً (توافق مع الواجهات القديمة).</summary>
    public string? GatewayReference { get; init; }
    public PaymentChannel PaymentChannel { get; init; } = PaymentChannel.Wallet;
}

public class RechargeCustomer360LineValidator : AbstractValidator<RechargeCustomer360LineRequest>
{
    public RechargeCustomer360LineValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مبلغ الشحن يجب أن يكون أكبر من صفر.");
    }
}

public class RechargeCustomer360LineHandler : IRequestHandler<RechargeCustomer360LineRequest, RechargeCustomer360LineResult>
{
    private readonly IPaymentServicesOrchestrator _orchestrator;
    private readonly IOperatorContext _operatorContext;

    public RechargeCustomer360LineHandler(
        IPaymentServicesOrchestrator orchestrator,
        IOperatorContext operatorContext)
    {
        _orchestrator = orchestrator;
        _operatorContext = operatorContext;
    }

    public async Task<RechargeCustomer360LineResult> Handle(
        RechargeCustomer360LineRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _operatorContext.UserId;
        var gatewayRef = string.IsNullOrWhiteSpace(request.GatewayReference)
            ? $"LEGACY-{Guid.CreateVersion7():N}"
            : request.GatewayReference.Trim();

        var draft = await _orchestrator.CreateRechargeDraftAsync(
            request.CustomerId,
            request.SubscriptionId,
            request.Amount,
            request.PaymentChannel,
            PaymentServiceChannel.Showroom,
            actorId,
            cancellationToken);

        var confirm = await _orchestrator.ConfirmAsync(
            draft.PaymentId,
            gatewayRef,
            actorId,
            cancellationToken);

        if (!confirm.Success)
        {
            throw new BusinessRuleViolationException(confirm.MessageAr ?? "تعذّر إتمام الشحن.");
        }

        return new RechargeCustomer360LineResult
        {
            Success = true,
            Msisdn = confirm.Msisdn,
            RechargedAmount = request.Amount,
            NewBalance = confirm.NewBalance ?? 0,
            Message = confirm.MessageAr,
            PaymentId = draft.PaymentId,
            PaymentNumber = confirm.PaymentNumber ?? draft.Number,
            ReceiptNumber = confirm.ReceiptNumber,
            CorrelationId = confirm.CorrelationId,
        };
    }
}
