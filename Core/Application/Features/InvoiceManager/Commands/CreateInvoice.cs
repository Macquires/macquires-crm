using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.InvoiceManager.Commands;

public class CreateInvoiceResult
{
    public Invoice? Data { get; set; }
}

public class CreateInvoiceRequest : IRequest<CreateInvoiceResult>
{
    public DateTime? InvoiceDate { get; init; }
    public string? InvoiceStatus { get; init; }
    public string? Description { get; init; }
    public string? SalesOrderId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.InvoiceDate).NotNull();
        RuleFor(x => x.InvoiceStatus).NotEmpty();
        RuleFor(x => x.SalesOrderId).NotEmpty();
    }
}

public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceRequest, CreateInvoiceResult>
{
    private readonly ICommandRepository<Invoice> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateInvoiceHandler(
        ICommandRepository<Invoice> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateInvoiceResult> Handle(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Invoice
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(Invoice), "", "INV"),
            InvoiceDate = request.InvoiceDate,
            InvoiceStatus = (InvoiceStatus)int.Parse(request.InvoiceStatus!),
            Description = request.Description,
            SalesOrderId = request.SalesOrderId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateInvoiceResult
        {
            Data = entity
        };
    }
}