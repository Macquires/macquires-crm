using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesTeamManager.Commands;

public class CreateSalesTeamResult
{
    public SalesTeam? Data { get; set; }
}

public class CreateSalesTeamRequest : IRequest<CreateSalesTeamResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateSalesTeamValidator : AbstractValidator<CreateSalesTeamRequest>
{
    public CreateSalesTeamValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateSalesTeamHandler : IRequestHandler<CreateSalesTeamRequest, CreateSalesTeamResult>
{
    private readonly ICommandRepository<SalesTeam> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSalesTeamHandler(
        ICommandRepository<SalesTeam> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateSalesTeamResult> Handle(CreateSalesTeamRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SalesTeam();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateSalesTeamResult
        {
            Data = entity
        };
    }
}