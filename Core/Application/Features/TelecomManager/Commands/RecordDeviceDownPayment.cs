using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom.DeviceSales;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class RecordDeviceDownPaymentResult
{
    public TelecomOperationRequest? Data { get; init; }
    public PaymentCaptureResult? GatewayResult { get; init; }
}

public class RecordDeviceDownPaymentRequest : IRequest<RecordDeviceDownPaymentResult>, IRequirePermission
{
    public string OperationId { get; init; } = null!;
    public string PaymentReference { get; init; } = null!;
    public decimal AmountPaid { get; init; }
    public PaymentChannel PaymentChannel { get; init; }

    public string PermissionKey => PermissionCatalog.TelecomDeviceSell;
}

public class RecordDeviceDownPaymentValidator : AbstractValidator<RecordDeviceDownPaymentRequest>
{
    public RecordDeviceDownPaymentValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.PaymentReference).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AmountPaid).GreaterThan(0);
    }
}

public class RecordDeviceDownPaymentHandler : IRequestHandler<RecordDeviceDownPaymentRequest, RecordDeviceDownPaymentResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayIntegration _paymentGateway;
    private readonly IOperatorContext _operator;

    public RecordDeviceDownPaymentHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        IUnitOfWork unitOfWork,
        IPaymentGatewayIntegration paymentGateway,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _operator = operatorContext;
    }

    public async Task<RecordDeviceDownPaymentResult> Handle(
        RecordDeviceDownPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (entity.Kind != TelecomOperationKind.DeviceSale)
        {
            throw new BusinessRuleViolationException("تسجيل الدفع متاح لبيع الأجهزة (DEV-) فقط.");
        }

        DeviceSaleOperationMutationGuard.EnsureEditable(entity);

        var gatewayResult = await _paymentGateway.CaptureAsync(
            new PaymentCaptureRequest(
                entity.Id,
                entity.Number,
                entity.CorrelationId,
                request.AmountPaid,
                request.PaymentChannel,
                request.PaymentReference.Trim()),
            cancellationToken);

        if (!gatewayResult.Success)
        {
            throw new BusinessRuleViolationException(gatewayResult.Message);
        }

        entity.PaymentReference = request.PaymentReference.Trim();
        entity.DeviceDownPaymentAmount = request.AmountPaid;
        entity.UpdatedById = OperatorActor.RequireUserId(_operator);
        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new RecordDeviceDownPaymentResult { Data = entity, GatewayResult = gatewayResult };
    }
}
