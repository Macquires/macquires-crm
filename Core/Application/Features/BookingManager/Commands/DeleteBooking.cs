using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingManager.Commands;

public class DeleteBookingResult
{
    public Booking? Data { get; set; }
}

public class DeleteBookingRequest : IRequest<DeleteBookingResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteBookingValidator : AbstractValidator<DeleteBookingRequest>
{
    public DeleteBookingValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteBookingHandler : IRequestHandler<DeleteBookingRequest, DeleteBookingResult>
{
    private readonly ICommandRepository<Booking> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookingHandler(
        ICommandRepository<Booking> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteBookingResult> Handle(DeleteBookingRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteBookingResult
        {
            Data = entity
        };
    }
}

