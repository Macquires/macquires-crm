using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.GeoCityManager.Commands;

public class UpdateGeoCityResult
{
    public GeoCity? Data { get; set; }
}

public class UpdateGeoCityRequest : IRequest<UpdateGeoCityResult>
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Governorate { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateGeoCityValidator : AbstractValidator<UpdateGeoCityRequest>
{
    public UpdateGeoCityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Governorate).NotEmpty();
    }
}

public class UpdateGeoCityHandler : IRequestHandler<UpdateGeoCityRequest, UpdateGeoCityResult>
{
    private readonly ICommandRepository<GeoCity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGeoCityHandler(
        ICommandRepository<GeoCity> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateGeoCityResult> Handle(UpdateGeoCityRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Name = request.Name;
        entity.Governorate = request.Governorate;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateGeoCityResult
        {
            Data = entity
        };
    }
}
