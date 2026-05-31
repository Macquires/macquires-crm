// Demo-only — customer tiering for Syriatel datasets.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class CustomerCategorySeeder
{
    private readonly ICommandRepository<CustomerCategory> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerCategorySeeder(
        ICommandRepository<CustomerCategory> categoryRepository,
        IUnitOfWork unitOfWork
    )
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var customerCategories = new List<CustomerCategory>
        {
            new CustomerCategory { Name = "شريحة كبار — Enterprise" },
            new CustomerCategory { Name = "شركات متوسطة" },
            new CustomerCategory { Name = "شركات صغيرة ومتاجر" },
            new CustomerCategory { Name = "شركات ناشئة" },
            new CustomerCategory { Name = "أفراد — استهلاك منزلي" }
        };

        foreach (var category in customerCategories)
        {
            await _categoryRepository.CreateAsync(category);
        }

        await _unitOfWork.SaveAsync();
    }
}
