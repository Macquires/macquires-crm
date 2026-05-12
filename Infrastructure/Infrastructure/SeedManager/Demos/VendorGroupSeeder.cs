// Demo-only — vendor types for telecom supply chain.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class VendorGroupSeeder
{
    private readonly ICommandRepository<VendorGroup> _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendorGroupSeeder(
        ICommandRepository<VendorGroup> groupRepository,
        IUnitOfWork unitOfWork
    )
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var vendorGroups = new List<VendorGroup>
        {
            new VendorGroup { Name = "مصنّع أجهزة وشبكات" },
            new VendorGroup { Name = "موزّع رئيسي" },
            new VendorGroup { Name = "مزوّد خدمات ولوجستيات" },
            new VendorGroup { Name = "مورّد قطع وملحقات" },
            new VendorGroup { Name = "مقاول ميداني" }
        };

        foreach (var group in vendorGroups)
        {
            await _groupRepository.CreateAsync(group);
        }

        await _unitOfWork.SaveAsync();
    }
}
