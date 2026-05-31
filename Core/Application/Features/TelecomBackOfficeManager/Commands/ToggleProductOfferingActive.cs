using Application.Common.Repositories;
using Application.Common.Services.ProductCatalog;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public class ToggleProductOfferingActiveResult
{
    public bool IsActive { get; init; }
}

public class ToggleProductOfferingActiveRequest : IRequest<ToggleProductOfferingActiveResult>
{
    public string Id { get; init; } = "";
    public string? UpdatedById { get; init; }
}

public class ToggleProductOfferingActiveValidator : AbstractValidator<ToggleProductOfferingActiveRequest>
{
    public ToggleProductOfferingActiveValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class ToggleProductOfferingActiveHandler : IRequestHandler<ToggleProductOfferingActiveRequest, ToggleProductOfferingActiveResult>
{
    private readonly ICommandRepository<ProductOffering> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActiveProductCatalogCache _catalogCache;

    public ToggleProductOfferingActiveHandler(
        ICommandRepository<ProductOffering> repository,
        IUnitOfWork unitOfWork,
        IActiveProductCatalogCache catalogCache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _catalogCache = catalogCache;
    }

    public async Task<ToggleProductOfferingActiveResult> Handle(ToggleProductOfferingActiveRequest request, CancellationToken cancellationToken)
    {
        var offering = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Product offering not found.");

        offering.IsActive = !offering.IsActive;
        offering.UpdatedById = request.UpdatedById;
        offering.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(offering);
        await _unitOfWork.SaveAsync(cancellationToken);
        _catalogCache.Invalidate();

        return new ToggleProductOfferingActiveResult { IsActive = offering.IsActive };
    }
}
