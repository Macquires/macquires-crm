using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentReceiveManager.Commands;

public class UpdatePaymentReceiveResult
{
    public PaymentReceive? Data { get; set; }
}

public class UpdatePaymentReceiveRequest : IRequest<UpdatePaymentReceiveResult>
{
    public string? Id { get; init; }
    public string? InvoiceId { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public double? PaymentAmount { get; init; }
    public string? Status { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdatePaymentReceiveValidator : AbstractValidator<UpdatePaymentReceiveRequest>
{
    public UpdatePaymentReceiveValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.PaymentDate).NotNull();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.PaymentAmount).NotNull();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class UpdatePaymentReceiveHandler : IRequestHandler<UpdatePaymentReceiveRequest, UpdatePaymentReceiveResult>
{
    private readonly ICommandRepository<PaymentReceive> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentReceiveHandler(
        ICommandRepository<PaymentReceive> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdatePaymentReceiveResult> Handle(UpdatePaymentReceiveRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.InvoiceId = request.InvoiceId;
        entity.Description = request.Description;
        entity.PaymentDate = request.PaymentDate;
        entity.PaymentMethodId = request.PaymentMethodId;
        entity.PaymentAmount = request.PaymentAmount;
        entity.Status = (PaymentReceiveStatus)int.Parse(request.Status!);

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdatePaymentReceiveResult
        {
            Data = entity
        };
    }
}