using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.CreditNoteManager.Commands;

public class UpdateCreditNoteResult
{
    public CreditNote? Data { get; set; }
}

public class UpdateCreditNoteRequest : IRequest<UpdateCreditNoteResult>
{
    public string? Id { get; init; }
    public DateTime? CreditNoteDate { get; init; }
    public string? CreditNoteStatus { get; init; }
    public string? Description { get; init; }
    public string? SalesReturnId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateCreditNoteValidator : AbstractValidator<UpdateCreditNoteRequest>
{
    public UpdateCreditNoteValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CreditNoteDate).NotNull();
        RuleFor(x => x.CreditNoteStatus).NotEmpty();
        RuleFor(x => x.SalesReturnId).NotEmpty();
    }
}

public class UpdateCreditNoteHandler : IRequestHandler<UpdateCreditNoteRequest, UpdateCreditNoteResult>
{
    private readonly ICommandRepository<CreditNote> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCreditNoteHandler(
        ICommandRepository<CreditNote> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateCreditNoteResult> Handle(UpdateCreditNoteRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.CreditNoteDate = request.CreditNoteDate;
        entity.CreditNoteStatus = (CreditNoteStatus)int.Parse(request.CreditNoteStatus!);
        entity.Description = request.Description;
        entity.SalesReturnId = request.SalesReturnId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateCreditNoteResult
        {
            Data = entity
        };
    }
}