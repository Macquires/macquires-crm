using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerContactManager.Commands;

public class DeleteCustomerContactResult
{
    public CustomerContact? Data { get; set; }
}

public class DeleteCustomerContactRequest : IRequest<DeleteCustomerContactResult>, IRequireAnyPermission
{
    public string? Id { get; init; }
    public IReadOnlyList<string> PermissionKeys => CustomerPermissionSets.ManageAny;
}

public class DeleteCustomerContactValidator : AbstractValidator<DeleteCustomerContactRequest>
{
    public DeleteCustomerContactValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteCustomerContactHandler : IRequestHandler<DeleteCustomerContactRequest, DeleteCustomerContactResult>
{
    private readonly ICommandRepository<CustomerContact> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public DeleteCustomerContactHandler(
        ICommandRepository<CustomerContact> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<DeleteCustomerContactResult> Handle(DeleteCustomerContactRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = OperatorActor.RequireUserId(_operator);

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteCustomerContactResult
        {
            Data = entity
        };
    }
}

