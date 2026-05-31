using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Seeds realistic Syriatel product offerings with components and price plans.
/// Offerings mirror real Syrian telecom market packages for demo/showroom presentation.
/// </summary>
public class ProductCatalogSeeder
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<ProductOffering> _offeringRepository;
    private readonly ICommandRepository<ProductOfferingComponent> _componentRepository;
    private readonly ICommandRepository<PricePlan> _pricePlanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductCatalogSeeder(
        IQueryContext query,
        ICommandRepository<ProductOffering> offeringRepository,
        ICommandRepository<ProductOfferingComponent> componentRepository,
        ICommandRepository<PricePlan> pricePlanRepository,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _offeringRepository = offeringRepository;
        _componentRepository = componentRepository;
        _pricePlanRepository = pricePlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        await EnsureCatalogAsync();
        await LinkDemoOfferingsToTechnicalProductsAsync();
    }

    /// <summary>Adds missing commercial offerings by <see cref="ProductOffering.Code"/> (idempotent).</summary>
    public async Task EnsureCatalogAsync()
    {
        var subTypes = await _query.TelecomSubscriptionTypeLookup.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .ToListAsync();

        string? TypeId(string code) => code switch
        {
            "PREPAID" => subTypes.FirstOrDefault(x => x.Code == "PREPAID")?.Id,
            "POSTPAID" => subTypes.FirstOrDefault(x => x.Code == "POSTPAID")?.Id,
            "HYBRID" => subTypes.FirstOrDefault(x => x.Code == "HYBRID")?.Id,
            _ => null,
        };

        var existingCodes = await _query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Select(o => o.Code)
            .ToListAsync();

        var existingSet = existingCodes.ToHashSet(StringComparer.Ordinal);

        foreach (var def in DemoSyrianTelecomCatalog.Offerings)
        {
            if (existingSet.Contains(def.Code))
            {
                continue;
            }

            var offering = new ProductOffering
            {
                Name = def.Name,
                NameEn = def.NameEn,
                Code = def.Code,
                Description = def.Description,
                CompatibleSubscriptionTypeId = TypeId(def.SubscriptionTypeCode),
                IsActive = true,
                SortOrder = def.SortOrder,
                PaymentType = def.PaymentType,
                BadgeColor = def.BadgeColor,
                ShortDescription = def.Description.Length > 120 ? def.Description[..120] + "…" : def.Description,
            };

            await _offeringRepository.CreateAsync(offering);
            await _unitOfWork.SaveAsync();

            await SeedComponentsAsync(offering.Id, def.Components);
            await SeedPricePlansAsync(offering.Id, def.Plans);
            existingSet.Add(def.Code);
        }
    }

    /// <summary>Maps demo offerings to seeded technical products by service code.</summary>
    public async Task LinkDemoOfferingsToTechnicalProductsAsync()
    {
        var offeringCodeToProductServiceCode = DemoSyrianTelecomCatalog.Offerings
            .ToDictionary(o => o.Code, o => o.ProductServiceCode, StringComparer.Ordinal);

        var serviceCodes = offeringCodeToProductServiceCode.Values.Distinct().ToList();
        var products = await _query.Product.AsNoTracking()
            .Where(p => !p.IsDeleted && p.ServiceCode != null && serviceCodes.Contains(p.ServiceCode))
            .ToListAsync();

        var productIdByServiceCode = products.ToDictionary(p => p.ServiceCode!, p => p.Id, StringComparer.Ordinal);

        foreach (var (offeringCode, productServiceCode) in offeringCodeToProductServiceCode)
        {
            if (!productIdByServiceCode.TryGetValue(productServiceCode, out var prodId))
            {
                continue;
            }

            var offering = await _query.ProductOffering.FirstOrDefaultAsync(
                o => !o.IsDeleted && o.Code == offeringCode);

            if (offering == null)
            {
                continue;
            }

            var tracked = await _offeringRepository.GetAsync(offering.Id);
            if (tracked == null)
            {
                continue;
            }

            if (string.Equals(tracked.ProductId, prodId, StringComparison.Ordinal))
            {
                continue;
            }

            tracked.ProductId = prodId;
            _offeringRepository.Update(tracked);
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task SeedComponentsAsync(
        string offeringId,
        (ServiceComponentType type, string label, decimal? quota, string? unit, bool unlimited)[] specs)
    {
        var sort = 0;
        foreach (var (type, label, quota, unit, unlimited) in specs)
        {
            await _componentRepository.CreateAsync(new ProductOfferingComponent
            {
                ProductOfferingId = offeringId,
                ComponentType = type,
                Label = label,
                Quota = quota,
                QuotaUnit = unit,
                IsUnlimited = unlimited,
                SortOrder = sort++,
            });
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task SeedPricePlansAsync(
        string offeringId,
        (PricePlanType planType, decimal price, decimal? activationFee, int? validityDays, bool isDefault)[] plans)
    {
        foreach (var (planType, price, activationFee, validityDays, isDefault) in plans)
        {
            await _pricePlanRepository.CreateAsync(new PricePlan
            {
                ProductOfferingId = offeringId,
                PlanType = planType,
                Price = price,
                CurrencyCode = "SYP",
                ActivationFee = activationFee,
                ValidityDays = validityDays,
                IsDefault = isDefault,
            });
        }

        await _unitOfWork.SaveAsync();
    }
}
