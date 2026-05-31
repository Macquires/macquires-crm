using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductCatalogManager.Commands;

// ─── Result ───
public class DeleteProductOfferingResult
{
    public ProductOffering? Data { get; set; }
}

// ─── Request ───
public class DeleteProductOfferingRequest : IRequest<DeleteProductOfferingResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

// ─── Validator ───
public class DeleteProductOfferingValidator : AbstractValidator<DeleteProductOfferingRequest>
{
    public DeleteProductOfferingValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

// ─── Handler ───
public class DeleteProductOfferingHandler : IRequestHandler<DeleteProductOfferingRequest, DeleteProductOfferingResult>
{
    private readonly ICommandRepository<ProductOffering> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductOfferingHandler(ICommandRepository<ProductOffering> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteProductOfferingResult> Handle(DeleteProductOfferingRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new InvalidOperationException("ProductOffering not found.");

        entity.UpdatedById = request.DeletedById;
        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteProductOfferingResult { Data = entity };
    }
}
