// Demo-only — aligned with Syria Telecom retail & B2B segments.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class CustomerGroupSeeder
{
    private readonly ICommandRepository<CustomerGroup> _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerGroupSeeder(
        ICommandRepository<CustomerGroup> groupRepository,
        IUnitOfWork unitOfWork
    )
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var customerGroups = new List<CustomerGroup>
        {
            new CustomerGroup { Name = "عملاء أفراد — تجزئة" },
            new CustomerGroup { Name = "شركات — أعمال" },
            new CustomerGroup { Name = "جهات حكومية" },
            new CustomerGroup { Name = "مؤسسات وجمعيات" },
            new CustomerGroup { Name = "قطاع تعليمي" },
            new CustomerGroup { Name = "قطاع ضيافة وسياحة" }
        };

        foreach (var group in customerGroups)
        {
            await _groupRepository.CreateAsync(group);
        }

        await _unitOfWork.SaveAsync();
    }
}
