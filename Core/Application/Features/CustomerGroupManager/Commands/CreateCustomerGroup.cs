using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerGroupManager.Commands;

public class CreateCustomerGroupResult
{
    public CustomerGroup? Data { get; set; }
}

public class CreateCustomerGroupRequest : IRequest<CreateCustomerGroupResult>, IRequireAnyPermission
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> PermissionKeys => ReferenceDataPermissionSets.ManageAny;
}

public class CreateCustomerGroupValidator : AbstractValidator<CreateCustomerGroupRequest>
{
    public CreateCustomerGroupValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateCustomerGroupHandler : IRequestHandler<CreateCustomerGroupRequest, CreateCustomerGroupResult>
{
    private readonly ICommandRepository<CustomerGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public CreateCustomerGroupHandler(
        ICommandRepository<CustomerGroup> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<CreateCustomerGroupResult> Handle(CreateCustomerGroupRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new CustomerGroup
        {
            CreatedById = OperatorActor.RequireUserId(_operator),
            Name = request.Name,
            Description = request.Description,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCustomerGroupResult
        {
            Data = entity
        };
    }
}
