using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingGroupManager.Commands;

public class DeleteBookingGroupResult
{
    public BookingGroup? Data { get; set; }
}

public class DeleteBookingGroupRequest : IRequest<DeleteBookingGroupResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteBookingGroupValidator : AbstractValidator<DeleteBookingGroupRequest>
{
    public DeleteBookingGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteBookingGroupHandler : IRequestHandler<DeleteBookingGroupRequest, DeleteBookingGroupResult>
{
    private readonly ICommandRepository<BookingGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookingGroupHandler(
        ICommandRepository<BookingGroup> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteBookingGroupResult> Handle(DeleteBookingGroupRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteBookingGroupResult
        {
            Data = entity
        };
    }
}

