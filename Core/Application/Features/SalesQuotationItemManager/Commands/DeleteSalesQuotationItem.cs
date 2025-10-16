using Application.Common.Repositories;
using Application.Features.SalesQuotationManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesQuotationItemManager.Commands;

public class DeleteSalesQuotationItemResult
{
    public SalesQuotationItem? Data { get; set; }
}

public class DeleteSalesQuotationItemRequest : IRequest<DeleteSalesQuotationItemResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteSalesQuotationItemValidator : AbstractValidator<DeleteSalesQuotationItemRequest>
{
    public DeleteSalesQuotationItemValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteSalesQuotationItemHandler : IRequestHandler<DeleteSalesQuotationItemRequest, DeleteSalesQuotationItemResult>
{
    private readonly ICommandRepository<SalesQuotationItem> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesQuotationService _salesQuotationService;

    public DeleteSalesQuotationItemHandler(
        ICommandRepository<SalesQuotationItem> repository,
        IUnitOfWork unitOfWork,
        SalesQuotationService salesQuotationService
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _salesQuotationService = salesQuotationService;
    }

    public async Task<DeleteSalesQuotationItemResult> Handle(DeleteSalesQuotationItemRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _salesQuotationService.Recalculate(entity.SalesQuotationId ?? "");

        return new DeleteSalesQuotationItemResult
        {
            Data = entity
        };
    }
}

