using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentReceiveManager.Commands;

public class CreatePaymentReceiveResult
{
    public PaymentReceive? Data { get; set; }
}

public class CreatePaymentReceiveRequest : IRequest<CreatePaymentReceiveResult>
{
    public string? InvoiceId { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public double? PaymentAmount { get; init; }
    public string? Status { get; init; }
    public string? CreatedById { get; init; }
}

public class CreatePaymentReceiveValidator : AbstractValidator<CreatePaymentReceiveRequest>
{
    public CreatePaymentReceiveValidator()
    {
        RuleFor(x => x.InvoiceId).NotNull();
        RuleFor(x => x.PaymentDate).NotNull();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.PaymentAmount).NotNull();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class CreatePaymentReceiveHandler : IRequestHandler<CreatePaymentReceiveRequest, CreatePaymentReceiveResult>
{
    private readonly ICommandRepository<PaymentReceive> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreatePaymentReceiveHandler(
        ICommandRepository<PaymentReceive> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreatePaymentReceiveResult> Handle(CreatePaymentReceiveRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new PaymentReceive
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(PaymentReceive), "", "PYRC"),
            InvoiceId = request.InvoiceId,
            Description = request.Description,
            PaymentDate = request.PaymentDate,
            PaymentMethodId = request.PaymentMethodId,
            PaymentAmount = request.PaymentAmount,
            Status = (PaymentReceiveStatus)int.Parse(request.Status!)
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePaymentReceiveResult
        {
            Data = entity
        };
    }
}