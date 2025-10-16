using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingResourceManager.Commands;

public class CreateBookingResourceResult
{
    public BookingResource? Data { get; set; }
}

public class CreateBookingResourceRequest : IRequest<CreateBookingResourceResult>
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? BookingGroupId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateBookingResourceValidator : AbstractValidator<CreateBookingResourceRequest>
{
    public CreateBookingResourceValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.BookingGroupId).NotEmpty();
    }
}

public class CreateBookingResourceHandler : IRequestHandler<CreateBookingResourceRequest, CreateBookingResourceResult>
{
    private readonly ICommandRepository<BookingResource> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBookingResourceHandler(
        ICommandRepository<BookingResource> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateBookingResourceResult> Handle(CreateBookingResourceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new BookingResource();
        entity.CreatedById = request.CreatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.BookingGroupId = request.BookingGroupId;

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateBookingResourceResult
        {
            Data = entity
        };
    }
}