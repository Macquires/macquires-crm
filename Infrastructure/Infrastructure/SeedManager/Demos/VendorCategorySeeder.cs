// Demo-only — vendor scale categories (telecom demo).
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class VendorCategorySeeder
{
    private readonly ICommandRepository<VendorCategory> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendorCategorySeeder(
        ICommandRepository<VendorCategory> categoryRepository,
        IUnitOfWork unitOfWork
    )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var vendorCategories = new List<VendorCategory>
        {
            new VendorCategory { Name = "شريك استراتيجي" },
            new VendorCategory { Name = "شريك إقليمي" },
            new VendorCategory { Name = "شريك محلي" },
            new VendorCategory { Name = "تخصصي — OEM" },
            new VendorCategory { Name = "خدمات ميدانية" }
        };

        foreach (var category in vendorCategories)
        {
            await _categoryRepository.CreateAsync(category);
        }

        await _unitOfWork.SaveAsync();
    }
}
