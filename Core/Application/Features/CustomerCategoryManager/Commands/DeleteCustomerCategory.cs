using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerCategoryManager.Commands;

public class DeleteCustomerCategoryResult
{
    public CustomerCategory? Data { get; set; }
}

public class DeleteCustomerCategoryRequest : IRequest<DeleteCustomerCategoryResult>, IRequireAnyPermission
{
    public string? Id { get; init; }
    public IReadOnlyList<string> PermissionKeys => ReferenceDataPermissionSets.ManageAny;
}

public class DeleteCustomerCategoryValidator : AbstractValidator<DeleteCustomerCategoryRequest>
{
    public DeleteCustomerCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteCustomerCategoryHandler : IRequestHandler<DeleteCustomerCategoryRequest, DeleteCustomerCategoryResult>
{
    private readonly ICommandRepository<CustomerCategory> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public DeleteCustomerCategoryHandler(
        ICommandRepository<CustomerCategory> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<DeleteCustomerCategoryResult> Handle(DeleteCustomerCategoryRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = OperatorActor.RequireUserId(_operator);

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteCustomerCategoryResult
        {
            Data = entity
        };
    }
}

