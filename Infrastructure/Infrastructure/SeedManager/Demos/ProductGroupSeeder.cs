// Demo-only — Syria Telecom product catalog segments.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class ProductGroupSeeder
{
    private readonly ICommandRepository<ProductGroup> _productGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductGroupSeeder(
        ICommandRepository<ProductGroup> productGroupRepository,
        IUnitOfWork unitOfWork
    )
    {
        _productGroupRepository = productGroupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var productGroups = new List<ProductGroup>
        {
            new ProductGroup { Name = "Mobile Lines", Description = "خطوط وباقات خط — سوريا تيليكوم (ديمو)" },
            new ProductGroup { Name = "Data Packages", Description = "حزم إنترنت منزلية وأعمال (ديمو)" },
            new ProductGroup { Name = "Hardware", Description = "أجهزة وصول — راوتر، Wingle، ملحقات (ديمو)" },
            new ProductGroup { Name = "Service", Description = "رسوم وخدمات مساعدة — تفعيل، تركيب، خصومات (ديمو)" }
        };

        foreach (var productGroup in productGroups)
        {
            await _productGroupRepository.CreateAsync(productGroup);
        }

        await _unitOfWork.SaveAsync();
    }
}
