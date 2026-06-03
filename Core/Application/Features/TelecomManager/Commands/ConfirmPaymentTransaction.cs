using Application.Common.Telecom.PaymentServices;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class ConfirmPaymentTransactionResult
{
    public bool Success { get; init; }
    public PaymentTransactionStatus Status { get; init; }
    public string? ReceiptNumber { get; init; }
    public decimal? NewBalance { get; init; }
    public string? CorrelationId { get; init; }
    public string? MessageAr { get; init; }
    public string? PaymentNumber { get; init; }
}

public class ConfirmPaymentTransactionRequest : IRequest<ConfirmPaymentTransactionResult>
{
    public string PaymentId { get; init; } = "";
    public string GatewayReference { get; init; } = "";
    public string? ConfirmedById { get; init; }
}

public class ConfirmPaymentTransactionValidator : AbstractValidator<ConfirmPaymentTransactionRequest>
{
    public ConfirmPaymentTransactionValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.GatewayReference).NotEmpty().MaximumLength(128);
    }
}

public class ConfirmPaymentTransactionHandler : IRequestHandler<ConfirmPaymentTransactionRequest, ConfirmPaymentTransactionResult>
{
    private readonly IPaymentServicesOrchestrator _orchestrator;

    public ConfirmPaymentTransactionHandler(IPaymentServicesOrchestrator orchestrator) => _orchestrator = orchestrator;

    public async Task<ConfirmPaymentTransactionResult> Handle(
        ConfirmPaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ConfirmAsync(
            request.PaymentId,
            request.GatewayReference,
            request.ConfirmedById,
            cancellationToken);

        return new ConfirmPaymentTransactionResult
        {
            Success = result.Success,
            Status = result.Status,
            ReceiptNumber = result.ReceiptNumber,
            NewBalance = result.NewBalance,
            CorrelationId = result.CorrelationId,
            MessageAr = result.MessageAr,
            PaymentNumber = result.PaymentNumber,
        };
    }
}
