using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesTeamManager.Commands;

public class DeleteSalesTeamResult
{
    public SalesTeam? Data { get; set; }
}

public class DeleteSalesTeamRequest : IRequest<DeleteSalesTeamResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteSalesTeamValidator : AbstractValidator<DeleteSalesTeamRequest>
{
    public DeleteSalesTeamValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteSalesTeamHandler : IRequestHandler<DeleteSalesTeamRequest, DeleteSalesTeamResult>
{
    private readonly ICommandRepository<SalesTeam> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSalesTeamHandler(
        ICommandRepository<SalesTeam> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteSalesTeamResult> Handle(DeleteSalesTeamRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteSalesTeamResult
        {
            Data = entity
        };
    }
}

