using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.PurchaseRequisitionManager.Commands;

public class UpdatePurchaseRequisitionResult
{
    public PurchaseRequisition? Data { get; set; }
}

public class UpdatePurchaseRequisitionRequest : IRequest<UpdatePurchaseRequisitionResult>
{
    public string? Id { get; init; }
    public DateTime? RequisitionDate { get; init; }
    public string? RequisitionStatus { get; init; }
    public string? Description { get; init; }
    public string? VendorId { get; init; }
    public string? TaxId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdatePurchaseRequisitionValidator : AbstractValidator<UpdatePurchaseRequisitionRequest>
{
    public UpdatePurchaseRequisitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequisitionDate).NotEmpty();
        RuleFor(x => x.RequisitionStatus).NotEmpty();
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.TaxId).NotEmpty();
    }
}

public class UpdatePurchaseRequisitionHandler : IRequestHandler<UpdatePurchaseRequisitionRequest, UpdatePurchaseRequisitionResult>
{
    private readonly ICommandRepository<PurchaseRequisition> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PurchaseRequisitionService _purchaseRequisitionService;

    public UpdatePurchaseRequisitionHandler(
        ICommandRepository<PurchaseRequisition> repository,
        PurchaseRequisitionService purchaseRequisitionService,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _purchaseRequisitionService = purchaseRequisitionService;
    }

    public async Task<UpdatePurchaseRequisitionResult> Handle(UpdatePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.RequisitionDate = request.RequisitionDate;
        entity.RequisitionStatus = (PurchaseRequisitionStatus)int.Parse(request.RequisitionStatus!);
        entity.Description = request.Description;
        entity.VendorId = request.VendorId;
        entity.TaxId = request.TaxId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _purchaseRequisitionService.Recalculate(entity.Id);

        return new UpdatePurchaseRequisitionResult
        {
            Data = entity
        };
    }
}

