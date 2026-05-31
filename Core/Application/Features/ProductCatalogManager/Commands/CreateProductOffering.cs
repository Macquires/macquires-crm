using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.ProductCatalogManager.Commands;

// ─── Result ───
public class CreateProductOfferingResult
{
    public ProductOffering? Data { get; set; }
}

// ─── Request ───
public class CreateProductOfferingRequest : IRequest<CreateProductOfferingResult>
{
    public string? Name { get; init; }
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public string? Code { get; init; }
    public string? CompatibleSubscriptionTypeId { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidToUtc { get; init; }
    public int SortOrder { get; init; }
    public string? CreatedById { get; init; }

    public string? EligibilityRules { get; init; }
    public string? AssetCompatibility { get; init; }
    public string? BillingCycle { get; init; }
    public string? TaxCategory { get; init; }
    public string? ServiceIdSocCode { get; init; }
    public double? SpeedQuotaLimitGb { get; init; }
    public int? VoiceMinutesLimit { get; init; }
    public string? ThrottlingPolicy { get; init; }
    public string? IconClass { get; init; }
    public string? BadgeColor { get; init; }
    public string? ShortDescription { get; init; }

    /// <summary>Optional technical product (CBS) this offering provisions against.</summary>
    public string? ProductId { get; init; }

    /// <summary>Inline components to create together with the offering (optional).</summary>
    public List<CreateProductOfferingComponentDto>? Components { get; init; }

    /// <summary>Inline price plans to create together with the offering (optional).</summary>
    public List<CreatePricePlanDto>? PricePlans { get; init; }
}

public record CreateProductOfferingComponentDto
{
    public ServiceComponentType ComponentType { get; init; }
    public string? Label { get; init; }
    public decimal? Quota { get; init; }
    public string? QuotaUnit { get; init; }
    public bool IsUnlimited { get; init; }
    public int SortOrder { get; init; }
}

public record CreatePricePlanDto
{
    public PricePlanType PlanType { get; init; }
    public decimal Price { get; init; }
    public string CurrencyCode { get; init; } = "SYP";
    public decimal? ActivationFee { get; init; }
    public int? ValidityDays { get; init; }
    public bool IsDefault { get; init; }
}

// ─── Validator ───
public class CreateProductOfferingValidator : AbstractValidator<CreateProductOfferingRequest>
{
    public CreateProductOfferingValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CompatibleSubscriptionTypeId)
            .NotEmpty()
            .WithMessage("يجب تحديد نوع الخط المتوافق مع الباقة (Prepaid/Postpaid/Hybrid).");

        RuleForEach(x => x.Components).ChildRules(comp =>
        {
            comp.RuleFor(c => c.Label).MaximumLength(255);
            comp.RuleFor(c => c.QuotaUnit).MaximumLength(50);
        });
        RuleForEach(x => x.PricePlans).ChildRules(pp =>
        {
            pp.RuleFor(p => p.Price).GreaterThanOrEqualTo(0);
            pp.RuleFor(p => p.CurrencyCode).NotEmpty().MaximumLength(50);
        });
        RuleFor(x => x.ProductId).MaximumLength(450);
    }
}

// ─── Handler ───
public class CreateProductOfferingHandler : IRequestHandler<CreateProductOfferingRequest, CreateProductOfferingResult>
{
    private readonly ICommandRepository<ProductOffering> _offeringRepository;
    private readonly ICommandRepository<ProductOfferingComponent> _componentRepository;
    private readonly ICommandRepository<PricePlan> _pricePlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductOfferingHandler(
        ICommandRepository<ProductOffering> offeringRepository,
        ICommandRepository<ProductOfferingComponent> componentRepository,
        ICommandRepository<PricePlan> pricePlanRepository,
        IUnitOfWork unitOfWork)
    {
        _offeringRepository = offeringRepository;
        _componentRepository = componentRepository;
        _pricePlanRepository = pricePlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateProductOfferingResult> Handle(CreateProductOfferingRequest request, CancellationToken cancellationToken)
    {
        var entity = new ProductOffering
        {
            CreatedById = request.CreatedById,
            Name = request.Name!,
            NameEn = request.NameEn,
            Description = request.Description,
            Code = request.Code!,
            CompatibleSubscriptionTypeId = request.CompatibleSubscriptionTypeId,
            IsActive = request.IsActive,
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc,
            SortOrder = request.SortOrder,

            EligibilityRules = request.EligibilityRules,
            AssetCompatibility = request.AssetCompatibility,
            BillingCycle = request.BillingCycle,
            TaxCategory = request.TaxCategory,
            ServiceIdSocCode = request.ServiceIdSocCode,
            SpeedQuotaLimitGb = request.SpeedQuotaLimitGb,
            VoiceMinutesLimit = request.VoiceMinutesLimit,
            ThrottlingPolicy = request.ThrottlingPolicy,
            IconClass = request.IconClass,
            BadgeColor = request.BadgeColor,
            ShortDescription = request.ShortDescription,
            ProductId = string.IsNullOrWhiteSpace(request.ProductId) ? null : request.ProductId.Trim()
        };

        await _offeringRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        // Create inline components if provided
        if (request.Components is { Count: > 0 })
        {
            foreach (var comp in request.Components)
            {
                var component = new ProductOfferingComponent
                {
                    CreatedById = request.CreatedById,
                    ProductOfferingId = entity.Id,
                    ComponentType = comp.ComponentType,
                    Label = comp.Label,
                    Quota = comp.Quota,
                    QuotaUnit = comp.QuotaUnit,
                    IsUnlimited = comp.IsUnlimited,
                    SortOrder = comp.SortOrder
                };
                await _componentRepository.CreateAsync(component, cancellationToken);
            }
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        // Create inline price plans if provided
        if (request.PricePlans is { Count: > 0 })
        {
            foreach (var pp in request.PricePlans)
            {
                var pricePlan = new PricePlan
                {
                    CreatedById = request.CreatedById,
                    ProductOfferingId = entity.Id,
                    PlanType = pp.PlanType,
                    Price = pp.Price,
                    CurrencyCode = pp.CurrencyCode,
                    ActivationFee = pp.ActivationFee,
                    ValidityDays = pp.ValidityDays,
                    IsDefault = pp.IsDefault
                };
                await _pricePlanRepository.CreateAsync(pricePlan, cancellationToken);
            }
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new CreateProductOfferingResult { Data = entity };
    }
}
