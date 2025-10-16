using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProgramManagerManager.Commands;

public class DeleteProgramManagerResult
{
    public ProgramManager? Data { get; set; }
}

public class DeleteProgramManagerRequest : IRequest<DeleteProgramManagerResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteProgramManagerValidator : AbstractValidator<DeleteProgramManagerRequest>
{
    public DeleteProgramManagerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteProgramManagerHandler : IRequestHandler<DeleteProgramManagerRequest, DeleteProgramManagerResult>
{
    private readonly ICommandRepository<ProgramManager> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProgramManagerHandler(
        ICommandRepository<ProgramManager> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteProgramManagerResult> Handle(DeleteProgramManagerRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteProgramManagerResult
        {
            Data = entity
        };
    }
}

