using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.GeoCityManager.Commands;

public class CreateGeoCityResult
{
    public GeoCity? Data { get; set; }
}

public class CreateGeoCityRequest : IRequest<CreateGeoCityResult>
{
    public string? Name { get; init; }
    public string? Governorate { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateGeoCityValidator : AbstractValidator<CreateGeoCityRequest>
{
    public CreateGeoCityValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Governorate).NotEmpty();
    }
}

public class CreateGeoCityHandler : IRequestHandler<CreateGeoCityRequest, CreateGeoCityResult>
{
    private readonly ICommandRepository<GeoCity> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGeoCityHandler(
        ICommandRepository<GeoCity> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateGeoCityResult> Handle(CreateGeoCityRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new GeoCity
        {
            CreatedById = request.CreatedById,
            Name = request.Name,
            Governorate = request.Governorate,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateGeoCityResult
        {
            Data = entity
        };
    }
}
