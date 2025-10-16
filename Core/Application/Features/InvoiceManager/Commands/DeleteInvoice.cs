using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.InvoiceManager.Commands;

public class DeleteInvoiceResult
{
    public Invoice? Data { get; set; }
}

public class DeleteInvoiceRequest : IRequest<DeleteInvoiceResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteInvoiceValidator : AbstractValidator<DeleteInvoiceRequest>
{
    public DeleteInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteInvoiceHandler : IRequestHandler<DeleteInvoiceRequest, DeleteInvoiceResult>
{
    private readonly ICommandRepository<Invoice> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteInvoiceHandler(
        ICommandRepository<Invoice> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteInvoiceResult> Handle(DeleteInvoiceRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteInvoiceResult
        {
            Data = entity
        };
    }
}