using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Telecom.DeviceSales;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class ApproveDeviceInstallmentResult
{
    public TelecomOperationRequest? Data { get; init; }
}

public class ApproveDeviceInstallmentRequest : IRequest<ApproveDeviceInstallmentResult>
{
    public string OperationId { get; init; } = null!;
    public bool Approved { get; init; }
    public string? Comments { get; init; }
    public string? UpdatedById { get; init; }
}

public class ApproveDeviceInstallmentValidator : AbstractValidator<ApproveDeviceInstallmentRequest>
{
    public ApproveDeviceInstallmentValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty();
    }
}

public class ApproveDeviceInstallmentHandler : IRequestHandler<ApproveDeviceInstallmentRequest, ApproveDeviceInstallmentResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveDeviceInstallmentHandler(ICommandRepository<TelecomOperationRequest> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApproveDeviceInstallmentResult> Handle(
        ApproveDeviceInstallmentRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.OperationId, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (entity.Kind != TelecomOperationKind.DeviceSale || entity.DeviceSaleType != DeviceSaleType.Installment)
        {
            throw new BusinessRuleViolationException("الاعتماد متاح لطلبات التقسيط فقط.");
        }

        DeviceSaleOperationMutationGuard.EnsureEditable(entity);

        if (!request.Approved)
        {
            entity.DeviceFinancingDecision = DeviceFinancingDecision.Rejected;
            entity.Notes = AppendNote(entity.Notes, $"[FinanceRejected] {request.Comments}");
        }
        else
        {
            entity.DeviceFinancingDecision = DeviceFinancingDecision.Approved;
            entity.Notes = AppendNote(entity.Notes, $"[FinanceApproved] {request.Comments}");
        }

        entity.UpdatedById = request.UpdatedById;
        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);
        return new ApproveDeviceInstallmentResult { Data = entity };
    }

    private static string AppendNote(string? existing, string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return existing ?? string.Empty;
        return string.IsNullOrEmpty(existing) ? line : $"{existing}\n{line}";
    }
}
