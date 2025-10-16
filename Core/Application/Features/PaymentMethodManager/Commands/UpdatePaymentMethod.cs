using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentMethodManager.Commands;

public class UpdatePaymentMethodResult
{
    public PaymentMethod? Data { get; set; }
}

public class UpdatePaymentMethodRequest : IRequest<UpdatePaymentMethodResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdatePaymentMethodValidator : AbstractValidator<UpdatePaymentMethodRequest>
{
    public UpdatePaymentMethodValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdatePaymentMethodHandler : IRequestHandler<UpdatePaymentMethodRequest, UpdatePaymentMethodResult>
{
    private readonly ICommandRepository<PaymentMethod> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentMethodHandler(
        ICommandRepository<PaymentMethod> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdatePaymentMethodResult> Handle(UpdatePaymentMethodRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdatePaymentMethodResult
        {
            Data = entity
        };
    }
}

