using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.CreditNoteManager.Commands;

public class CreateCreditNoteResult
{
    public CreditNote? Data { get; set; }
}

public class CreateCreditNoteRequest : IRequest<CreateCreditNoteResult>
{
    public DateTime? CreditNoteDate { get; init; }
    public string? CreditNoteStatus { get; init; }
    public string? Description { get; init; }
    public string? SalesReturnId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateCreditNoteValidator : AbstractValidator<CreateCreditNoteRequest>
{
    public CreateCreditNoteValidator()
    {
        RuleFor(x => x.CreditNoteDate).NotNull();
        RuleFor(x => x.CreditNoteStatus).NotEmpty();
        RuleFor(x => x.SalesReturnId).NotEmpty();
    }
}

public class CreateCreditNoteHandler : IRequestHandler<CreateCreditNoteRequest, CreateCreditNoteResult>
{
    private readonly ICommandRepository<CreditNote> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateCreditNoteHandler(
        ICommandRepository<CreditNote> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateCreditNoteResult> Handle(CreateCreditNoteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new CreditNote
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(CreditNote), "", "CN"),
            CreditNoteDate = request.CreditNoteDate,
            CreditNoteStatus = (CreditNoteStatus)int.Parse(request.CreditNoteStatus!),
            Description = request.Description,
            SalesReturnId = request.SalesReturnId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateCreditNoteResult
        {
            Data = entity
        };
    }
}