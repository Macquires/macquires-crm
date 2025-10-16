using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingGroupManager.Commands;

public class UpdateBookingGroupResult
{
    public BookingGroup? Data { get; set; }
}

public class UpdateBookingGroupRequest : IRequest<UpdateBookingGroupResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateBookingGroupValidator : AbstractValidator<UpdateBookingGroupRequest>
{
    public UpdateBookingGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class UpdateBookingGroupHandler : IRequestHandler<UpdateBookingGroupRequest, UpdateBookingGroupResult>
{
    private readonly ICommandRepository<BookingGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBookingGroupHandler(
        ICommandRepository<BookingGroup> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateBookingGroupResult> Handle(UpdateBookingGroupRequest request, CancellationToken cancellationToken)
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

        return new UpdateBookingGroupResult
        {
            Data = entity
        };
    }
}

