using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentMethodManager.Commands;

public class DeletePaymentMethodResult
{
    public PaymentMethod? Data { get; set; }
}

public class DeletePaymentMethodRequest : IRequest<DeletePaymentMethodResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePaymentMethodValidator : AbstractValidator<DeletePaymentMethodRequest>
{
    public DeletePaymentMethodValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePaymentMethodHandler : IRequestHandler<DeletePaymentMethodRequest, DeletePaymentMethodResult>
{
    private readonly ICommandRepository<PaymentMethod> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePaymentMethodHandler(
        ICommandRepository<PaymentMethod> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePaymentMethodResult> Handle(DeletePaymentMethodRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePaymentMethodResult
        {
            Data = entity
        };
    }
}

