using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.SalesQuotationManager.Commands;

public class DeleteSalesQuotationResult
{
    public SalesQuotation? Data { get; set; }
}

public class DeleteSalesQuotationRequest : IRequest<DeleteSalesQuotationResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteSalesQuotationValidator : AbstractValidator<DeleteSalesQuotationRequest>
{
    public DeleteSalesQuotationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteSalesQuotationHandler : IRequestHandler<DeleteSalesQuotationRequest, DeleteSalesQuotationResult>
{
    private readonly ICommandRepository<SalesQuotation> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSalesQuotationHandler(
        ICommandRepository<SalesQuotation> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteSalesQuotationResult> Handle(DeleteSalesQuotationRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteSalesQuotationResult
        {
            Data = entity
        };
    }
}

