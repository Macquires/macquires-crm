using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesRepresentativeManager.Commands;

public class UpdateSalesRepresentativeResult
{
    public SalesRepresentative? Data { get; set; }
}

public class UpdateSalesRepresentativeRequest : IRequest<UpdateSalesRepresentativeResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? JobTitle { get; init; }
    public string? EmployeeNumber { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Description { get; init; }
    public string? SalesTeamId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateSalesRepresentativeValidator : AbstractValidator<UpdateSalesRepresentativeRequest>
{
    public UpdateSalesRepresentativeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.JobTitle).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.EmailAddress).NotEmpty();
        RuleFor(x => x.SalesTeamId).NotEmpty();
    }
}

public class UpdateSalesRepresentativeHandler : IRequestHandler<UpdateSalesRepresentativeRequest, UpdateSalesRepresentativeResult>
{
    private readonly ICommandRepository<SalesRepresentative> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesRepresentativeHandler(
        ICommandRepository<SalesRepresentative> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateSalesRepresentativeResult> Handle(UpdateSalesRepresentativeRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Name = request.Name;
        entity.JobTitle = request.JobTitle;
        entity.EmployeeNumber = request.EmployeeNumber;
        entity.PhoneNumber = request.PhoneNumber;
        entity.EmailAddress = request.EmailAddress;
        entity.Description = request.Description;
        entity.SalesTeamId = request.SalesTeamId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateSalesRepresentativeResult
        {
            Data = entity
        };
    }
}