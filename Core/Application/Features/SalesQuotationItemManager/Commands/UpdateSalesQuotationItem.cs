using Application.Common.Repositories;
using Application.Features.SalesQuotationManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesQuotationItemManager.Commands;

public class UpdateSalesQuotationItemResult
{
    public SalesQuotationItem? Data { get; set; }
}

public class UpdateSalesQuotationItemRequest : IRequest<UpdateSalesQuotationItemResult>
{
    public string? Id { get; init; }
    public string? SalesQuotationId { get; init; }
    public string? ProductId { get; init; }
    public string? Summary { get; init; }
    public double? UnitPrice { get; init; }
    public double? Quantity { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateSalesQuotationItemValidator : AbstractValidator<UpdateSalesQuotationItemRequest>
{
    public UpdateSalesQuotationItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SalesQuotationId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
        RuleFor(x => x.Quantity).NotEmpty();
    }
}

public class UpdateSalesQuotationItemHandler : IRequestHandler<UpdateSalesQuotationItemRequest, UpdateSalesQuotationItemResult>
{
    private readonly ICommandRepository<SalesQuotationItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesQuotationService _salesQuotationService;

    public UpdateSalesQuotationItemHandler(
        ICommandRepository<SalesQuotationItem> repository,
        IUnitOfWork unitOfWork,
        SalesQuotationService salesQuotationService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _salesQuotationService = salesQuotationService;
    }

    public async Task<UpdateSalesQuotationItemResult> Handle(UpdateSalesQuotationItemRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.SalesQuotationId = request.SalesQuotationId;
        entity.ProductId = request.ProductId;
        entity.Summary = request.Summary;
        entity.UnitPrice = request.UnitPrice;
        entity.Quantity = request.Quantity;

        entity.Total = entity.UnitPrice * entity.Quantity;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _salesQuotationService.Recalculate(entity.SalesQuotationId ?? "");

        return new UpdateSalesQuotationItemResult
        {
            Data = entity
        };
    }
}

