using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentMethodManager.Commands;

public class CreatePaymentMethodResult
{
    public PaymentMethod? Data { get; set; }
}

public class CreatePaymentMethodRequest : IRequest<CreatePaymentMethodResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class CreatePaymentMethodValidator : AbstractValidator<CreatePaymentMethodRequest>
{
    public CreatePaymentMethodValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreatePaymentMethodHandler : IRequestHandler<CreatePaymentMethodRequest, CreatePaymentMethodResult>
{
    private readonly ICommandRepository<PaymentMethod> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentMethodHandler(
        ICommandRepository<PaymentMethod> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatePaymentMethodResult> Handle(CreatePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new PaymentMethod();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreatePaymentMethodResult
        {
            Data = entity
        };
    }
}