using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerGroupManager.Commands;

public class DeleteCustomerGroupResult
{
    public CustomerGroup? Data { get; set; }
}

public class DeleteCustomerGroupRequest : IRequest<DeleteCustomerGroupResult>, IRequireAnyPermission
{
    public string? Id { get; init; }
    public IReadOnlyList<string> PermissionKeys => ReferenceDataPermissionSets.ManageAny;
}

public class DeleteCustomerGroupValidator : AbstractValidator<DeleteCustomerGroupRequest>
{
    public DeleteCustomerGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteCustomerGroupHandler : IRequestHandler<DeleteCustomerGroupRequest, DeleteCustomerGroupResult>
{
    private readonly ICommandRepository<CustomerGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public DeleteCustomerGroupHandler(
        ICommandRepository<CustomerGroup> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<DeleteCustomerGroupResult> Handle(DeleteCustomerGroupRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = OperatorActor.RequireUserId(_operator);

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteCustomerGroupResult
        {
            Data = entity
        };
    }
}

