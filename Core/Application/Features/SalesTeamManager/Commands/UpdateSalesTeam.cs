using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesTeamManager.Commands;

public class UpdateSalesTeamResult
{
    public SalesTeam? Data { get; set; }
}

public class UpdateSalesTeamRequest : IRequest<UpdateSalesTeamResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateSalesTeamValidator : AbstractValidator<UpdateSalesTeamRequest>
{
    public UpdateSalesTeamValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateSalesTeamHandler : IRequestHandler<UpdateSalesTeamRequest, UpdateSalesTeamResult>
{
    private readonly ICommandRepository<SalesTeam> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesTeamHandler(
        ICommandRepository<SalesTeam> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateSalesTeamResult> Handle(UpdateSalesTeamRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateSalesTeamResult
        {
            Data = entity
        };
    }
}

