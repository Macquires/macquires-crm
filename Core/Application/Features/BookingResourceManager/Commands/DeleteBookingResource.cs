using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.BookingResourceManager.Commands;

public class DeleteBookingResourceResult
{
    public BookingResource? Data { get; set; }
}

public class DeleteBookingResourceRequest : IRequest<DeleteBookingResourceResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteBookingResourceValidator : AbstractValidator<DeleteBookingResourceRequest>
{
    public DeleteBookingResourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteBookingResourceHandler : IRequestHandler<DeleteBookingResourceRequest, DeleteBookingResourceResult>
{
    private readonly ICommandRepository<BookingResource> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookingResourceHandler(
        ICommandRepository<BookingResource> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteBookingResourceResult> Handle(DeleteBookingResourceRequest request, CancellationToken cancellationToken)
    {

        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteBookingResourceResult
        {
            Data = entity
        };
    }
}

