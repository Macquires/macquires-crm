using Application.Common.Repositories;
using Application.Features.PurchaseRequisitionManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PurchaseRequisitionItemManager.Commands;

public class UpdatePurchaseRequisitionItemResult
{
    public PurchaseRequisitionItem? Data { get; set; }
}

public class UpdatePurchaseRequisitionItemRequest : IRequest<UpdatePurchaseRequisitionItemResult>
{
    public string? Id { get; init; }
    public string? PurchaseRequisitionId { get; init; }
    public string? ProductId { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdatePurchaseRequisitionItemValidator : AbstractValidator<UpdatePurchaseRequisitionItemRequest>
{
    public UpdatePurchaseRequisitionItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PurchaseRequisitionId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
        RuleFor(x => x.Quantity).NotEmpty();
    }
}

public class UpdatePurchaseRequisitionItemHandler : IRequestHandler<UpdatePurchaseRequisitionItemRequest, UpdatePurchaseRequisitionItemResult>
{
    private readonly ICommandRepository<PurchaseRequisitionItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PurchaseRequisitionService _purchaseRequisitionService;

    public UpdatePurchaseRequisitionItemHandler(
        ICommandRepository<PurchaseRequisitionItem> repository,
        IUnitOfWork unitOfWork,
        PurchaseRequisitionService purchaseRequisitionService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _purchaseRequisitionService = purchaseRequisitionService;
    }

    public async Task<UpdatePurchaseRequisitionItemResult> Handle(UpdatePurchaseRequisitionItemRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.PurchaseRequisitionId = request.PurchaseRequisitionId;
        entity.ProductId = request.ProductId;
        entity.Summary = request.Summary;
        entity.UnitPrice = request.UnitPrice;
        entity.Quantity = request.Quantity;

        entity.Total = entity.UnitPrice * entity.Quantity;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _purchaseRequisitionService.Recalculate(entity.PurchaseRequisitionId ?? "");

        return new UpdatePurchaseRequisitionItemResult
        {
            Data = entity
        };
    }
}

