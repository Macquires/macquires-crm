using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerCategoryManager.Commands;

public class CreateCustomerCategoryResult
{
    public CustomerCategory? Data { get; set; }
}

public class CreateCustomerCategoryRequest : IRequest<CreateCustomerCategoryResult>, IRequireAnyPermission
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> PermissionKeys => ReferenceDataPermissionSets.ManageAny;
}

public class CreateCustomerCategoryValidator : AbstractValidator<CreateCustomerCategoryRequest>
{
    public CreateCustomerCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateCustomerCategoryHandler : IRequestHandler<CreateCustomerCategoryRequest, CreateCustomerCategoryResult>
{
    private readonly ICommandRepository<CustomerCategory> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public CreateCustomerCategoryHandler(
        ICommandRepository<CustomerCategory> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<CreateCustomerCategoryResult> Handle(CreateCustomerCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new CustomerCategory
        {
            CreatedById = OperatorActor.RequireUserId(_operator),
            Name = request.Name,
            Description = request.Description,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCustomerCategoryResult
        {
            Data = entity
        };
    }
}
