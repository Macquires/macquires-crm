using Application.Common.Repositories;
using Application.Common.Security;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductManager.Commands;

public class CreateProductResult
{
    public Product? Data { get; set; }
}

public class CreateProductRequest : IRequest<CreateProductResult>, IRequireAnyPermission
{
    public string? Number { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public double? UnitPrice { get; init; }
    public bool? Physical { get; init; } = false;
    public string? CompatibleSubscriptionTypeId { get; init; }
    public string? ServiceCode { get; init; }

    public IReadOnlyList<string> PermissionKeys => ProductCatalogPermissionSets.ManageAny;
}

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.UnitPrice).NotEmpty();
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductRequest, CreateProductResult>
{
    private readonly ICommandRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IOperatorContext _operator;

    public CreateProductHandler(
        ICommandRepository<Product> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _operator = operatorContext;
    }

    public async Task<CreateProductResult> Handle(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Product
        {
            CreatedById = OperatorActor.RequireUserId(_operator),
            Number = await _numberSequenceService.GenerateNumberAsync(nameof(Product), "", "SVC", cancellationToken: cancellationToken),
            Name = request.Name,
            UnitPrice = request.UnitPrice,
            Physical = request.Physical ?? false,
            Description = request.Description,
            ServiceCode = request.ServiceCode,
            CompatibleSubscriptionTypeId = string.IsNullOrWhiteSpace(request.CompatibleSubscriptionTypeId)
                ? null
                : request.CompatibleSubscriptionTypeId.Trim()
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return new CreateProductResult { Data = entity };
    }
}
