using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesQuotationManager.Commands;

public class CreateSalesQuotationResult
{
    public SalesQuotation? Data { get; set; }
}

public class CreateSalesQuotationRequest : IRequest<CreateSalesQuotationResult>
{
    public DateTime? QuotationDate { get; init; }
    public string? QuotationStatus { get; init; }
    public string? Description { get; init; }
    public string? CustomerId { get; init; }
    public string? TaxId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateSalesQuotationValidator : AbstractValidator<CreateSalesQuotationRequest>
{
    public CreateSalesQuotationValidator()
    {
        RuleFor(x => x.QuotationDate).NotEmpty();
        RuleFor(x => x.QuotationStatus).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.TaxId).NotEmpty();
    }
}

public class CreateSalesQuotationHandler : IRequestHandler<CreateSalesQuotationRequest, CreateSalesQuotationResult>
{
    private readonly ICommandRepository<SalesQuotation> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly SalesQuotationService _salesQuotationService;

    public CreateSalesQuotationHandler(
        ICommandRepository<SalesQuotation> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        SalesQuotationService salesQuotationService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _salesQuotationService = salesQuotationService;
    }

    public async Task<CreateSalesQuotationResult> Handle(CreateSalesQuotationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SalesQuotation();
        entity.CreatedById = request.CreatedById;

        entity.Number = _numberSequenceService.GenerateNumber(nameof(SalesQuotation), "", "SQ");
        entity.QuotationDate = request.QuotationDate;
        entity.QuotationStatus = (SalesQuotationStatus)int.Parse(request.QuotationStatus!);
        entity.Description = request.Description;
        entity.CustomerId = request.CustomerId;
        entity.TaxId = request.TaxId;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        _salesQuotationService.Recalculate(entity.Id);

        return new CreateSalesQuotationResult
        {
            Data = entity
        };
    }
}