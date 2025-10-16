using Application.Common.Repositories;
using Application.Features.PurchaseRequisitionManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PurchaseRequisitionItemManager.Commands;

public class DeletePurchaseRequisitionItemResult
{
    public PurchaseRequisitionItem? Data { get; set; }
}

public class DeletePurchaseRequisitionItemRequest : IRequest<DeletePurchaseRequisitionItemResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePurchaseRequisitionItemValidator : AbstractValidator<DeletePurchaseRequisitionItemRequest>
{
    public DeletePurchaseRequisitionItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePurchaseRequisitionItemHandler : IRequestHandler<DeletePurchaseRequisitionItemRequest, DeletePurchaseRequisitionItemResult>
{
    private readonly ICommandRepository<PurchaseRequisitionItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PurchaseRequisitionService _purchaseRequisitionService;

    public DeletePurchaseRequisitionItemHandler(
        ICommandRepository<PurchaseRequisitionItem> repository,
        IUnitOfWork unitOfWork,
        PurchaseRequisitionService purchaseRequisitionService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _purchaseRequisitionService = purchaseRequisitionService;
    }

    public async Task<DeletePurchaseRequisitionItemResult> Handle(DeletePurchaseRequisitionItemRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _purchaseRequisitionService.Recalculate(entity.PurchaseRequisitionId ?? "");

        return new DeletePurchaseRequisitionItemResult
        {
            Data = entity
        };
    }
}

