using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CompanyManager.Commands;

public class UpdateCompanyResult
{
    public Company? Data { get; set; }
}

public class UpdateCompanyRequest : IRequest<UpdateCompanyResult>, IRequirePermission
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Currency { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public string? PhoneNumber { get; init; }
    public string? FaxNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Website { get; init; }
    public string PermissionKey => PermissionCatalog.AdminSettingsManage;
}

public class UpdateCompanyValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.ZipCode).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.EmailAddress).NotEmpty();
    }
}

public class UpdateCompanyHandler : IRequestHandler<UpdateCompanyRequest, UpdateCompanyResult>
{
    private readonly ICommandRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public UpdateCompanyHandler(
        ICommandRepository<Company> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<UpdateCompanyResult> Handle(UpdateCompanyRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = OperatorActor.RequireUserId(_operator);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Currency = request.Currency;
        entity.Street = request.Street;
        entity.City = request.City;
        entity.State = request.State;
        entity.ZipCode = request.ZipCode;
        entity.Country = request.Country;
        entity.PhoneNumber = request.PhoneNumber;
        entity.FaxNumber = request.FaxNumber;
        entity.EmailAddress = request.EmailAddress;
        entity.Website = request.Website;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateCompanyResult
        {
            Data = entity
        };
    }
}

