using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.ProgramManagerManager.Commands;

public class CreateProgramManagerResult
{
    public ProgramManager? Data { get; set; }
}

public class CreateProgramManagerRequest : IRequest<CreateProgramManagerResult>
{
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? ProgramManagerResourceId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateProgramManagerValidator : AbstractValidator<CreateProgramManagerRequest>
{
    public CreateProgramManagerValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty();
        RuleFor(x => x.ProgramManagerResourceId).NotEmpty();
    }
}

public class CreateProgramManagerHandler : IRequestHandler<CreateProgramManagerRequest, CreateProgramManagerResult>
{
    private readonly ICommandRepository<ProgramManager> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateProgramManagerHandler(
        ICommandRepository<ProgramManager> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateProgramManagerResult> Handle(CreateProgramManagerRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ProgramManager();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(ProgramManager), "", "PRG");
        entity.Title = request.Title;
        entity.Summary = request.Summary;
        entity.Status = (ProgramManagerStatus)int.Parse(request.Status!);
        entity.Priority = (ProgramManagerPriority)int.Parse(request.Priority!);
        entity.ProgramManagerResourceId = request.ProgramManagerResourceId;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateProgramManagerResult
        {
            Data = entity
        };
    }
}