using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.InvoiceManager.Commands;

public class UpdateInvoiceResult
{
    public Invoice? Data { get; set; }
}

public class UpdateInvoiceRequest : IRequest<UpdateInvoiceResult>
{
    public string? Id { get; init; }
    public DateTime? InvoiceDate { get; init; }
    public string? InvoiceStatus { get; init; }
    public string? Description { get; init; }
    public string? SalesOrderId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InvoiceDate).NotNull();
        RuleFor(x => x.InvoiceStatus).NotEmpty();
        RuleFor(x => x.SalesOrderId).NotEmpty();
    }
}

public class UpdateInvoiceHandler : IRequestHandler<UpdateInvoiceRequest, UpdateInvoiceResult>
{
    private readonly ICommandRepository<Invoice> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateInvoiceHandler(
        ICommandRepository<Invoice> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateInvoiceResult> Handle(UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.InvoiceDate = request.InvoiceDate;
        entity.InvoiceStatus = (InvoiceStatus)int.Parse(request.InvoiceStatus!);
        entity.Description = request.Description;
        entity.SalesOrderId = request.SalesOrderId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateInvoiceResult
        {
            Data = entity
        };
    }
}