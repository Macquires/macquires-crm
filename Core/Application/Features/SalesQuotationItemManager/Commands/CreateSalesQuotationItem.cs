using Application.Common.Repositories;
using Application.Features.SalesQuotationManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesQuotationItemManager.Commands;

public class CreateSalesQuotationItemResult
{
    public SalesQuotationItem? Data { get; set; }
}

public class CreateSalesQuotationItemRequest : IRequest<CreateSalesQuotationItemResult>
{
    public string? SalesQuotationId { get; init; }
    public string? ProductId { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateSalesQuotationItemValidator : AbstractValidator<CreateSalesQuotationItemRequest>
{
    public CreateSalesQuotationItemValidator()
    {
        RuleFor(x => x.SalesQuotationId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
        RuleFor(x => x.Quantity).NotEmpty();
    }
}

public class CreateSalesQuotationItemHandler : IRequestHandler<CreateSalesQuotationItemRequest, CreateSalesQuotationItemResult>
{
    private readonly ICommandRepository<SalesQuotationItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesQuotationService _salesQuotationService;

    public CreateSalesQuotationItemHandler(
        ICommandRepository<SalesQuotationItem> repository,
        IUnitOfWork unitOfWork,
        SalesQuotationService salesQuotationService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _salesQuotationService = salesQuotationService;
    }

    public async Task<CreateSalesQuotationItemResult> Handle(CreateSalesQuotationItemRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SalesQuotationItem();
        entity.CreatedById = request.CreatedById;

        entity.SalesQuotationId = request.SalesQuotationId;
        entity.ProductId = request.ProductId;
        entity.Summary = request.Summary;
        entity.UnitPrice = request.UnitPrice;
        entity.Quantity = request.Quantity;

        entity.Total = entity.Quantity * entity.UnitPrice;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        _salesQuotationService.Recalculate(entity.SalesQuotationId ?? "");

        return new CreateSalesQuotationItemResult
        {
            Data = entity
        };
    }
}