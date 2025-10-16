using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentDisburseManager.Commands;

public class UpdatePaymentDisburseResult
{
    public PaymentDisburse? Data { get; set; }
}

public class UpdatePaymentDisburseRequest : IRequest<UpdatePaymentDisburseResult>
{
    public string? Id { get; init; }
    public string? BillId { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public double? PaymentAmount { get; init; }
    public string? Status { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdatePaymentDisburseValidator : AbstractValidator<UpdatePaymentDisburseRequest>
{
    public UpdatePaymentDisburseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BillId).NotNull();
        RuleFor(x => x.PaymentDate).NotNull();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.PaymentAmount).NotNull();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class UpdatePaymentDisburseHandler : IRequestHandler<UpdatePaymentDisburseRequest, UpdatePaymentDisburseResult>
{
    private readonly ICommandRepository<PaymentDisburse> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentDisburseHandler(
        ICommandRepository<PaymentDisburse> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdatePaymentDisburseResult> Handle(UpdatePaymentDisburseRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.BillId = request.BillId;
        entity.Description = request.Description;
        entity.PaymentDate = request.PaymentDate;
        entity.PaymentMethodId = request.PaymentMethodId;
        entity.PaymentAmount = request.PaymentAmount;
        entity.Status = (PaymentDisburseStatus)int.Parse(request.Status!);

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdatePaymentDisburseResult
        {
            Data = entity
        };
    }
}