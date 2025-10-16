using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesRepresentativeManager.Commands;

public class CreateSalesRepresentativeResult
{
    public SalesRepresentative? Data { get; set; }
}

public class CreateSalesRepresentativeRequest : IRequest<CreateSalesRepresentativeResult>
{
    public string? Name { get; init; }
    public string? JobTitle { get; init; }
    public string? EmployeeNumber { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Description { get; init; }
    public string? SalesTeamId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateSalesRepresentativeValidator : AbstractValidator<CreateSalesRepresentativeRequest>
{
    public CreateSalesRepresentativeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.JobTitle).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.EmailAddress).NotEmpty();
        RuleFor(x => x.SalesTeamId).NotEmpty();
    }
}

public class CreateSalesRepresentativeHandler : IRequestHandler<CreateSalesRepresentativeRequest, CreateSalesRepresentativeResult>
{
    private readonly ICommandRepository<SalesRepresentative> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateSalesRepresentativeHandler(
        ICommandRepository<SalesRepresentative> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateSalesRepresentativeResult> Handle(CreateSalesRepresentativeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SalesRepresentative
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(SalesRepresentative), "", "SR"),
            Name = request.Name,
            JobTitle = request.JobTitle,
            EmployeeNumber = request.EmployeeNumber,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.EmailAddress,
            Description = request.Description,
            SalesTeamId = request.SalesTeamId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateSalesRepresentativeResult
        {
            Data = entity
        };
    }
}