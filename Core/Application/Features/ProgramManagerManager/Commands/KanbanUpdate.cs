using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ProgramManagerManager.Commands;

public class KanbanTask
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Title { get; set; }
    public string? Number { get; set; }
    public string? Summary { get; set; }
    public string? ProgramManagerResource { get; set; }
    public string? ProgramManagerResourceId { get; set; }
}

public class KanbanUpdateResult
{
    public KanbanTask? Data { get; set; }
}

public class KanbanUpdateRequest : IRequest<KanbanUpdateResult>
{
    public string? Action { get; set; }
    public string? Key { get; set; }
    public string? KeyColumn { get; set; }
    public KanbanTask? Value { get; set; }
}

public class KanbanUpdateValidator : AbstractValidator<KanbanUpdateRequest>
{
    public KanbanUpdateValidator()
    {
    }
}

public class KanbanUpdateHandler : IRequestHandler<KanbanUpdateRequest, KanbanUpdateResult>
{
    private readonly ICommandRepository<ProgramManager> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public KanbanUpdateHandler(
        ICommandRepository<ProgramManager> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<KanbanUpdateResult> Handle(KanbanUpdateRequest request, CancellationToken cancellationToken)
    {

        var value = request.Value;
        if (value != null)
        {
            var existing = await _repository
                .GetQuery()
                .IsDeletedEqualTo(false)
                .Where(x => x.Number == value.Number)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != null)
            {
                existing.Title = value.Title ?? existing.Number;
                existing.Summary = value.Summary ?? string.Empty;
                existing.ProgramManagerResourceId = value.ProgramManagerResourceId;
                existing.Status = (ProgramManagerStatus)Enum.Parse(typeof(ProgramManagerStatus), value.Status ?? string.Empty);
                existing.Priority = (ProgramManagerPriority)Enum.Parse(typeof(ProgramManagerPriority), value.Priority ?? string.Empty);

                _repository.Update(existing);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }


        return new KanbanUpdateResult
        {
            Data = value
        };
    }
}

