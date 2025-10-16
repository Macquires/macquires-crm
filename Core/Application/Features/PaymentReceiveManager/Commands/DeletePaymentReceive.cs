using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.PaymentReceiveManager.Commands;

public class DeletePaymentReceiveResult
{
    public PaymentReceive? Data { get; set; }
}

public class DeletePaymentReceiveRequest : IRequest<DeletePaymentReceiveResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeletePaymentReceiveValidator : AbstractValidator<DeletePaymentReceiveRequest>
{
    public DeletePaymentReceiveValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeletePaymentReceiveHandler : IRequestHandler<DeletePaymentReceiveRequest, DeletePaymentReceiveResult>
{
    private readonly ICommandRepository<PaymentReceive> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePaymentReceiveHandler(
        ICommandRepository<PaymentReceive> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeletePaymentReceiveResult> Handle(DeletePaymentReceiveRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeletePaymentReceiveResult
        {
            Data = entity
        };
    }
}