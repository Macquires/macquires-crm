using Application.Common.Repositories;
using Application.Common.Security;
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
public class DeleteProductOfferingRequest : IRequest<DeleteProductOfferingResult>, IRequireAnyPermission
{
    public string? Id { get; init; }
    public IReadOnlyList<string> PermissionKeys => ProductCatalogPermissionSets.ManageAny;
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
    private readonly IOperatorContext _operator;

    public DeleteProductOfferingHandler(
        ICommandRepository<ProductOffering> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<DeleteProductOfferingResult> Handle(DeleteProductOfferingRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new InvalidOperationException("ProductOffering not found.");

        entity.UpdatedById = OperatorActor.RequireUserId(_operator);
        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteProductOfferingResult { Data = entity };
    }
}
