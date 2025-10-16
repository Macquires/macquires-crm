using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.DebitNoteManager.Commands;

public class DeleteDebitNoteResult
{
    public DebitNote? Data { get; set; }
}

public class DeleteDebitNoteRequest : IRequest<DeleteDebitNoteResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteDebitNoteValidator : AbstractValidator<DeleteDebitNoteRequest>
{
    public DeleteDebitNoteValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteDebitNoteHandler : IRequestHandler<DeleteDebitNoteRequest, DeleteDebitNoteResult>
{
    private readonly ICommandRepository<DebitNote> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDebitNoteHandler(
        ICommandRepository<DebitNote> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteDebitNoteResult> Handle(DeleteDebitNoteRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteDebitNoteResult
        {
            Data = entity
        };
    }
}