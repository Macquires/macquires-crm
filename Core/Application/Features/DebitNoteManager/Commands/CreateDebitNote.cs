using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.DebitNoteManager.Commands;

public class CreateDebitNoteResult
{
    public DebitNote? Data { get; set; }
}

public class CreateDebitNoteRequest : IRequest<CreateDebitNoteResult>
{
    public DateTime? DebitNoteDate { get; init; }
    public string? DebitNoteStatus { get; init; }
    public string? Description { get; init; }
    public string? PurchaseReturnId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateDebitNoteValidator : AbstractValidator<CreateDebitNoteRequest>
{
    public CreateDebitNoteValidator()
    {
        RuleFor(x => x.DebitNoteDate).NotNull();
        RuleFor(x => x.DebitNoteStatus).NotEmpty();
        RuleFor(x => x.PurchaseReturnId).NotEmpty();
    }
}

public class CreateDebitNoteHandler : IRequestHandler<CreateDebitNoteRequest, CreateDebitNoteResult>
{
    private readonly ICommandRepository<DebitNote> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateDebitNoteHandler(
        ICommandRepository<DebitNote> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateDebitNoteResult> Handle(CreateDebitNoteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new DebitNote
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(DebitNote), "", "DN"),
            DebitNoteDate = request.DebitNoteDate,
            DebitNoteStatus = (DebitNoteStatus)int.Parse(request.DebitNoteStatus!),
            Description = request.Description,
            PurchaseReturnId = request.PurchaseReturnId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateDebitNoteResult
        {
            Data = entity
        };
    }
}