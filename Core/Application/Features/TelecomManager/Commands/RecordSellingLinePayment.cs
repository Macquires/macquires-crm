using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom.SellingLine;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class RecordSellingLinePaymentResult
{
    public TelecomOperationRequest? Data { get; init; }
    public PaymentCaptureResult? GatewayResult { get; init; }
}

public class RecordSellingLinePaymentRequest : IRequest<RecordSellingLinePaymentResult>, IRequireAnyPermission
{
    public string OperationId { get; init; } = null!;
    public string PaymentReference { get; init; } = null!;
    public decimal AmountPaid { get; init; }
    public PaymentChannel PaymentChannel { get; init; }

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.ConfirmAny;
}

public class RecordSellingLinePaymentValidator : AbstractValidator<RecordSellingLinePaymentRequest>
{
    public RecordSellingLinePaymentValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.PaymentReference).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AmountPaid).GreaterThan(0);
    }
}

public class RecordSellingLinePaymentHandler : IRequestHandler<RecordSellingLinePaymentRequest, RecordSellingLinePaymentResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGatewayIntegration _paymentGateway;
    private readonly IQueryContext _query;
    private readonly IOperatorContext _operator;

    public RecordSellingLinePaymentHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        IUnitOfWork unitOfWork,
        IPaymentGatewayIntegration paymentGateway,
        IQueryContext query,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _query = query;
        _operator = operatorContext;
    }

    public async Task<RecordSellingLinePaymentResult> Handle(
        RecordSellingLinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (entity.Kind != TelecomOperationKind.NewActivation)
        {
            throw new BusinessRuleViolationException("تسجيل الدفع متاح لعمليات تفعيل خط جديد فقط.");
        }

        SellingLineOperationMutationGuard.EnsureEditableInventoryFields(
            entity, entity.MsisdnAssetId, entity.SimInventoryId, entity.ProductOfferingId, entity.ProductId);

        if (entity.Status is TelecomOperationStatus.Completed or TelecomOperationStatus.Failed)
        {
            throw new BusinessRuleViolationException("لا يمكن تسجيل دفع لعملية منتهية.");
        }

        var requiredDeposit = await SellingLineDepositResolver.ResolveRequiredDepositAsync(
            _query, entity, cancellationToken);
        if (requiredDeposit > 0 && request.AmountPaid < requiredDeposit)
        {
            throw new BusinessRuleViolationException(
                "VAL-02-05: المبلغ المدفوع أقل من قيمة الوديعة المطلوبة للباقة.");
        }

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
        entity.InitialDepositAmount = request.AmountPaid;
        entity.UpdatedById = OperatorActor.RequireUserId(_operator);
        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new RecordSellingLinePaymentResult { Data = entity, GatewayResult = gatewayResult };
    }
}
