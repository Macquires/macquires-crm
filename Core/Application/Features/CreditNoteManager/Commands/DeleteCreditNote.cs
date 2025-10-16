using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.CreditNoteManager.Commands;

public class DeleteCreditNoteResult
{
    public CreditNote? Data { get; set; }
}

public class DeleteCreditNoteRequest : IRequest<DeleteCreditNoteResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteCreditNoteValidator : AbstractValidator<DeleteCreditNoteRequest>
{
    public DeleteCreditNoteValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteCreditNoteHandler : IRequestHandler<DeleteCreditNoteRequest, DeleteCreditNoteResult>
{
    private readonly ICommandRepository<CreditNote> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCreditNoteHandler(
        ICommandRepository<CreditNote> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteCreditNoteResult> Handle(DeleteCreditNoteRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteCreditNoteResult
        {
            Data = entity
        };
    }
}