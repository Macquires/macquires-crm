using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesQuotationManager.Commands;

public class UpdateSalesQuotationResult
{
    public SalesQuotation? Data { get; set; }
}

public class UpdateSalesQuotationRequest : IRequest<UpdateSalesQuotationResult>
{
    public string? Id { get; init; }
    public DateTime? QuotationDate { get; init; }
    public string? QuotationStatus { get; init; }
    public string? Description { get; init; }
    public string? CustomerId { get; init; }
    public string? TaxId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateSalesQuotationValidator : AbstractValidator<UpdateSalesQuotationRequest>
{
    public UpdateSalesQuotationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.QuotationDate).NotEmpty();
        RuleFor(x => x.QuotationStatus).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.TaxId).NotEmpty();
    }
}

public class UpdateSalesQuotationHandler : IRequestHandler<UpdateSalesQuotationRequest, UpdateSalesQuotationResult>
{
    private readonly ICommandRepository<SalesQuotation> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesQuotationService _salesQuotationService;

    public UpdateSalesQuotationHandler(
        ICommandRepository<SalesQuotation> repository,
        SalesQuotationService salesQuotationService,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _salesQuotationService = salesQuotationService;
    }

    public async Task<UpdateSalesQuotationResult> Handle(UpdateSalesQuotationRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.QuotationDate = request.QuotationDate;
        entity.QuotationStatus = (SalesQuotationStatus)int.Parse(request.QuotationStatus!);
        entity.Description = request.Description;
        entity.CustomerId = request.CustomerId;
        entity.TaxId = request.TaxId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _salesQuotationService.Recalculate(entity.Id);

        return new UpdateSalesQuotationResult
        {
            Data = entity
        };
    }
}

