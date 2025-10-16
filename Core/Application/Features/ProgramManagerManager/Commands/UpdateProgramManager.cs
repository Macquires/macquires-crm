using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.ProgramManagerManager.Commands;

public class UpdateProgramManagerResult
{
    public ProgramManager? Data { get; set; }
}

public class UpdateProgramManagerRequest : IRequest<UpdateProgramManagerResult>
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? ProgramManagerResourceId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateProgramManagerValidator : AbstractValidator<UpdateProgramManagerRequest>
{
    public UpdateProgramManagerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty();
        RuleFor(x => x.ProgramManagerResourceId).NotEmpty();
    }
}

public class UpdateProgramManagerHandler : IRequestHandler<UpdateProgramManagerRequest, UpdateProgramManagerResult>
{
    private readonly ICommandRepository<ProgramManager> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProgramManagerHandler(
        ICommandRepository<ProgramManager> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateProgramManagerResult> Handle(UpdateProgramManagerRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Title = request.Title;
        entity.Summary = request.Summary;
        entity.Status = (ProgramManagerStatus)int.Parse(request.Status!);
        entity.Priority = (ProgramManagerPriority)int.Parse(request.Priority!);
        entity.ProgramManagerResourceId = request.ProgramManagerResourceId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateProgramManagerResult
        {
            Data = entity
        };
    }
}

