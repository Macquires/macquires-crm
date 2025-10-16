using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentDisburseManager.Commands;

public class DeletePaymentDisburseResult
{
    public PaymentDisburse? Data { get; set; }
}

public class DeletePaymentDisburseRequest : IRequest<DeletePaymentDisburseResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePaymentDisburseValidator : AbstractValidator<DeletePaymentDisburseRequest>
{
    public DeletePaymentDisburseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePaymentDisburseHandler : IRequestHandler<DeletePaymentDisburseRequest, DeletePaymentDisburseResult>
{
    private readonly ICommandRepository<PaymentDisburse> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePaymentDisburseHandler(
        ICommandRepository<PaymentDisburse> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePaymentDisburseResult> Handle(DeletePaymentDisburseRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePaymentDisburseResult
        {
            Data = entity
        };
    }
}