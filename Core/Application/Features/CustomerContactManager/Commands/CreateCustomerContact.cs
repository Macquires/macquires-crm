using Application.Common.Repositories;
using Application.Common.Security;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerContactManager.Commands;

public class CreateCustomerContactResult
{
    public CustomerContact? Data { get; set; }
}

public class CreateCustomerContactRequest : IRequest<CreateCustomerContactResult>, IRequireAnyPermission
{
    public string? Name { get; init; }
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Description { get; set; }
    public string? CustomerId { get; set; }
    public IReadOnlyList<string> PermissionKeys => CustomerPermissionSets.ManageAny;
}

public class CreateCustomerContactValidator : AbstractValidator<CreateCustomerContactRequest>
{
    public CreateCustomerContactValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.JobTitle).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.EmailAddress).NotEmpty();
    }
}

public class CreateCustomerContactHandler : IRequestHandler<CreateCustomerContactRequest, CreateCustomerContactResult>
{
    private readonly ICommandRepository<CustomerContact> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IOperatorContext _operator;

    public CreateCustomerContactHandler(
        ICommandRepository<CustomerContact> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _operator = operatorContext;
    }

    public async Task<CreateCustomerContactResult> Handle(CreateCustomerContactRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new CustomerContact
        {
            CreatedById = OperatorActor.RequireUserId(_operator),
            Name = request.Name,
            Number = await _numberSequenceService.GenerateNumberAsync(nameof(CustomerContact), "", "CC", cancellationToken: cancellationToken),
            JobTitle = request.JobTitle,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.EmailAddress,
            Description = request.Description,
            CustomerId = request.CustomerId,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCustomerContactResult
        {
            Data = entity
        };
    }
}
