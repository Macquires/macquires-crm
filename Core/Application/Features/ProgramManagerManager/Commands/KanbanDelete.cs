using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProgramManagerManager.Commands;


public class KanbanDeleteResult
{
    public string? Data { get; set; }
}

public class KanbanDeleteRequest : IRequest<KanbanDeleteResult>
{
    public string? Action { get; set; }
    public string? Key { get; set; }
    public string? KeyColumn { get; set; }
    public KanbanTask? Value { get; set; }
}

public class KanbanDeleteValidator : AbstractValidator<KanbanDeleteRequest>
{
    public KanbanDeleteValidator()
    {
    }
}

public class KanbanDeleteHandler : IRequestHandler<KanbanDeleteRequest, KanbanDeleteResult>
{
    private readonly ICommandRepository<ProgramManager> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public KanbanDeleteHandler(
        ICommandRepository<ProgramManager> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<KanbanDeleteResult> Handle(KanbanDeleteRequest request, CancellationToken cancellationToken)
    {

        var key = request.Key;
        if (key != null)
        {
            var existing = await _repository.GetAsync(key, cancellationToken);

            if (existing != null)
            {
                _repository.Delete(existing);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }


        return new KanbanDeleteResult
        {
            Data = key
        };
    }
}

