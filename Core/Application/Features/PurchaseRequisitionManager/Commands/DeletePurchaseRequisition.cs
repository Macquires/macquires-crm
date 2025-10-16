using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PurchaseRequisitionManager.Commands;

public class DeletePurchaseRequisitionResult
{
    public PurchaseRequisition? Data { get; set; }
}

public class DeletePurchaseRequisitionRequest : IRequest<DeletePurchaseRequisitionResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePurchaseRequisitionValidator : AbstractValidator<DeletePurchaseRequisitionRequest>
{
    public DeletePurchaseRequisitionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePurchaseRequisitionHandler : IRequestHandler<DeletePurchaseRequisitionRequest, DeletePurchaseRequisitionResult>
{
    private readonly ICommandRepository<PurchaseRequisition> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePurchaseRequisitionHandler(
        ICommandRepository<PurchaseRequisition> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePurchaseRequisitionResult> Handle(DeletePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePurchaseRequisitionResult
        {
            Data = entity
        };
    }
}

