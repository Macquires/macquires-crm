using Application.Common.Integrations;
using Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public class ValidateVoucherResult
{
    public bool Valid { get; init; }
    public string MessageAr { get; init; } = "";
    public decimal? FaceValue { get; init; }
}

public class ValidateVoucherRequest : IRequest<ValidateVoucherResult>, IRequireAnyPermission
{
    public string VoucherCode { get; init; } = "";
    public decimal? Amount { get; init; }

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.PaymentAny;
}

public class ValidateVoucherValidator : AbstractValidator<ValidateVoucherRequest>
{
    public ValidateVoucherValidator() => RuleFor(x => x.VoucherCode).NotEmpty().MaximumLength(64);
}

public class ValidateVoucherHandler : IRequestHandler<ValidateVoucherRequest, ValidateVoucherResult>
{
    private readonly IPaymentGatewayIntegration _paymentGateway;

    public ValidateVoucherHandler(IPaymentGatewayIntegration paymentGateway) => _paymentGateway = paymentGateway;

    public async Task<ValidateVoucherResult> Handle(ValidateVoucherRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentGateway.ValidateVoucherAsync(
            new VoucherValidationRequest(request.VoucherCode.Trim(), request.Amount),
            cancellationToken);

        return new ValidateVoucherResult
        {
            Valid = result.Valid,
            MessageAr = result.MessageAr,
            FaceValue = result.FaceValue,
        };
    }
}
