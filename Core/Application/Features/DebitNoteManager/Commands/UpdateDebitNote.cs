using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.DebitNoteManager.Commands;

public class UpdateDebitNoteResult
{
    public DebitNote? Data { get; set; }
}

public class UpdateDebitNoteRequest : IRequest<UpdateDebitNoteResult>
{
    public string? Id { get; init; }
    public DateTime? DebitNoteDate { get; init; }
    public string? DebitNoteStatus { get; init; }
    public string? Description { get; init; }
    public string? PurchaseReturnId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateDebitNoteValidator : AbstractValidator<UpdateDebitNoteRequest>
{
    public UpdateDebitNoteValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DebitNoteDate).NotNull();
        RuleFor(x => x.DebitNoteStatus).NotEmpty();
        RuleFor(x => x.PurchaseReturnId).NotEmpty();
    }
}

public class UpdateDebitNoteHandler : IRequestHandler<UpdateDebitNoteRequest, UpdateDebitNoteResult>
{
    private readonly ICommandRepository<DebitNote> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDebitNoteHandler(
        ICommandRepository<DebitNote> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateDebitNoteResult> Handle(UpdateDebitNoteRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.DebitNoteDate = request.DebitNoteDate;
        entity.DebitNoteStatus = (DebitNoteStatus)int.Parse(request.DebitNoteStatus!);
        entity.Description = request.Description;
        entity.PurchaseReturnId = request.PurchaseReturnId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateDebitNoteResult
        {
            Data = entity
        };
    }
}