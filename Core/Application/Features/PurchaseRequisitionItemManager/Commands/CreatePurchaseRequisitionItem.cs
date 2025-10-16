using Application.Common.Repositories;
using Application.Features.PurchaseRequisitionManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PurchaseRequisitionItemManager.Commands;

public class CreatePurchaseRequisitionItemResult
{
    public PurchaseRequisitionItem? Data { get; set; }
}

public class CreatePurchaseRequisitionItemRequest : IRequest<CreatePurchaseRequisitionItemResult>
{
    public string? PurchaseRequisitionId { get; init; }
    public string? ProductId { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public string? CreatedById { get; init; }
}

public class CreatePurchaseRequisitionItemValidator : AbstractValidator<CreatePurchaseRequisitionItemRequest>
{
    public CreatePurchaseRequisitionItemValidator()
    {
        RuleFor(x => x.PurchaseRequisitionId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
        RuleFor(x => x.Quantity).NotEmpty();
    }
}

public class CreatePurchaseRequisitionItemHandler : IRequestHandler<CreatePurchaseRequisitionItemRequest, CreatePurchaseRequisitionItemResult>
{
    private readonly ICommandRepository<PurchaseRequisitionItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PurchaseRequisitionService _purchaseRequisitionService;

    public CreatePurchaseRequisitionItemHandler(
        ICommandRepository<PurchaseRequisitionItem> repository,
        IUnitOfWork unitOfWork,
        PurchaseRequisitionService purchaseRequisitionService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _purchaseRequisitionService = purchaseRequisitionService;
    }

    public async Task<CreatePurchaseRequisitionItemResult> Handle(CreatePurchaseRequisitionItemRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new PurchaseRequisitionItem();
        entity.CreatedById = request.CreatedById;

        entity.PurchaseRequisitionId = request.PurchaseRequisitionId;
        entity.ProductId = request.ProductId;
        entity.Summary = request.Summary;
        entity.UnitPrice = request.UnitPrice;
        entity.Quantity = request.Quantity;

        entity.Total = entity.Quantity * entity.UnitPrice;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        _purchaseRequisitionService.Recalculate(entity.PurchaseRequisitionId ?? "");

        return new CreatePurchaseRequisitionItemResult
        {
            Data = entity
        };
    }
}