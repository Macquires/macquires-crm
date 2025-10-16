using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingResourceManager.Commands;

public class UpdateBookingResourceResult
{
    public BookingResource? Data { get; set; }
}

public class UpdateBookingResourceRequest : IRequest<UpdateBookingResourceResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? BookingGroupId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateBookingResourceValidator : AbstractValidator<UpdateBookingResourceRequest>
{
    public UpdateBookingResourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.BookingGroupId).NotEmpty();
    }
}

public class UpdateBookingResourceHandler : IRequestHandler<UpdateBookingResourceRequest, UpdateBookingResourceResult>
{
    private readonly ICommandRepository<BookingResource> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBookingResourceHandler(
        ICommandRepository<BookingResource> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateBookingResourceResult> Handle(UpdateBookingResourceRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.BookingGroupId = request.BookingGroupId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateBookingResourceResult
        {
            Data = entity
        };
    }
}

