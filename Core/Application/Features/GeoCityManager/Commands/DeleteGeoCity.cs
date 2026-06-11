using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.GeoCityManager.Commands;

public class DeleteGeoCityResult
{
    public GeoCity? Data { get; set; }
}

public class DeleteGeoCityRequest : IRequest<DeleteGeoCityResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
}

public class DeleteGeoCityValidator : AbstractValidator<DeleteGeoCityRequest>
{
    public DeleteGeoCityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteGeoCityHandler : IRequestHandler<DeleteGeoCityRequest, DeleteGeoCityResult>
{
    private readonly ICommandRepository<GeoCity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGeoCityHandler(
        ICommandRepository<GeoCity> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteGeoCityResult> Handle(DeleteGeoCityRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.DeletedById;

        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DeleteGeoCityResult
        {
            Data = entity
        };
    }
}
