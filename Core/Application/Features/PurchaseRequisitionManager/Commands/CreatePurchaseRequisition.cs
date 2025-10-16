using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.PurchaseRequisitionManager.Commands;

public class CreatePurchaseRequisitionResult
{
    public PurchaseRequisition? Data { get; set; }
}

public class CreatePurchaseRequisitionRequest : IRequest<CreatePurchaseRequisitionResult>
{
    public DateTime? RequisitionDate { get; init; }
    public string? RequisitionStatus { get; init; }
    public string? Description { get; init; }
    public string? VendorId { get; init; }
    public string? TaxId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreatePurchaseRequisitionValidator : AbstractValidator<CreatePurchaseRequisitionRequest>
{
    public CreatePurchaseRequisitionValidator()
    {
        RuleFor(x => x.RequisitionDate).NotEmpty();
        RuleFor(x => x.RequisitionStatus).NotEmpty();
        RuleFor(x => x.VendorId).NotEmpty();
        RuleFor(x => x.TaxId).NotEmpty();
    }
}

public class CreatePurchaseRequisitionHandler : IRequestHandler<CreatePurchaseRequisitionRequest, CreatePurchaseRequisitionResult>
{
    private readonly ICommandRepository<PurchaseRequisition> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly PurchaseRequisitionService _purchaseRequisitionService;

    public CreatePurchaseRequisitionHandler(
        ICommandRepository<PurchaseRequisition> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        PurchaseRequisitionService purchaseRequisitionService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _purchaseRequisitionService = purchaseRequisitionService;
    }

    public async Task<CreatePurchaseRequisitionResult> Handle(CreatePurchaseRequisitionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new PurchaseRequisition();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(PurchaseRequisition), "", "PR");
        entity.RequisitionDate = request.RequisitionDate;
        entity.RequisitionStatus = (PurchaseRequisitionStatus)int.Parse(request.RequisitionStatus!);
        entity.Description = request.Description;
        entity.VendorId = request.VendorId;
        entity.TaxId = request.TaxId;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        _purchaseRequisitionService.Recalculate(entity.Id);

        return new CreatePurchaseRequisitionResult
        {
            Data = entity
        };
    }
}