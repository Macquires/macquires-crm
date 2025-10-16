using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingGroupManager.Commands;

public class CreateBookingGroupResult
{
    public BookingGroup? Data { get; set; }
}

public class CreateBookingGroupRequest : IRequest<CreateBookingGroupResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateBookingGroupValidator : AbstractValidator<CreateBookingGroupRequest>
{
    public CreateBookingGroupValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class CreateBookingGroupHandler : IRequestHandler<CreateBookingGroupRequest, CreateBookingGroupResult>
{
    private readonly ICommandRepository<BookingGroup> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBookingGroupHandler(
        ICommandRepository<BookingGroup> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateBookingGroupResult> Handle(CreateBookingGroupRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new BookingGroup();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateBookingGroupResult
        {
            Data = entity
        };
    }
}