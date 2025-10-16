using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentDisburseManager.Commands;

public class CreatePaymentDisburseResult
{
    public PaymentDisburse? Data { get; set; }
}

public class CreatePaymentDisburseRequest : IRequest<CreatePaymentDisburseResult>
{
    public string? BillId { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public double? PaymentAmount { get; init; }
    public string? Status { get; init; }
    public string? CreatedById { get; init; }
}

public class CreatePaymentDisburseValidator : AbstractValidator<CreatePaymentDisburseRequest>
{
    public CreatePaymentDisburseValidator()
    {
        RuleFor(x => x.BillId).NotNull();
        RuleFor(x => x.PaymentDate).NotNull();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.PaymentAmount).NotNull();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class CreatePaymentDisburseHandler : IRequestHandler<CreatePaymentDisburseRequest, CreatePaymentDisburseResult>
{
    private readonly ICommandRepository<PaymentDisburse> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreatePaymentDisburseHandler(
        ICommandRepository<PaymentDisburse> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreatePaymentDisburseResult> Handle(CreatePaymentDisburseRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new PaymentDisburse
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(PaymentDisburse), "", "PYDS"),
            BillId = request.BillId,
            Description = request.Description,
            PaymentDate = request.PaymentDate,
            PaymentMethodId = request.PaymentMethodId,
            PaymentAmount = request.PaymentAmount,
            Status = (PaymentDisburseStatus)int.Parse(request.Status!)
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePaymentDisburseResult
        {
            Data = entity
        };
    }
}