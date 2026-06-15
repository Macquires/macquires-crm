using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class ProductSeeder
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public ProductSeeder(
        IQueryContext query,
        ICommandRepository<Product> productRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _productRepository = productRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        await EnsureProductsAsync();
    }

    /// <summary>Adds missing Syrian demo technical products by <see cref="Product.ServiceCode"/> (idempotent).</summary>
    public async Task EnsureProductsAsync()
    {
        var subTypes = await _query.TelecomSubscriptionTypeLookup.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .ToListAsync();

        string? TypeId(string? code) => code switch
        {
            "PREPAID" => subTypes.FirstOrDefault(x => x.Code == "PREPAID")?.Id,
            "POSTPAID" => subTypes.FirstOrDefault(x => x.Code == "POSTPAID")?.Id,
            "HYBRID" => subTypes.FirstOrDefault(x => x.Code == "HYBRID")?.Id,
            _ => null,
        };

        var existingCodes = await _query.Product.AsNoTracking()
            .Where(p => !p.IsDeleted && p.ServiceCode != null)
            .Select(p => p.ServiceCode!)
            .ToListAsync();

        var existingSet = existingCodes.ToHashSet(StringComparer.Ordinal);
        var added = false;

        foreach (var def in DemoSyrianTelecomCatalog.Products)
        {
            if (existingSet.Contains(def.ServiceCode))
            {
                continue;
            }

            var product = new Product
            {
                Name = def.Name,
                ServiceCode = def.ServiceCode,
                UnitPrice = def.UnitPrice,
                Physical = def.ServiceCode.StartsWith("HW_", StringComparison.Ordinal),
                CompatibleSubscriptionTypeId = TypeId(def.SubscriptionTypeCode),
            };
            product.Number = await _numberSequenceService.GenerateNumberAsync(nameof(Product), "", "SVC");
            await _productRepository.CreateAsync(product);
            existingSet.Add(def.ServiceCode);
            added = true;
        }

        if (added)
        {
            await _unitOfWork.SaveAsync();
        }
    }
}
