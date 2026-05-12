// Demo-only fictional operator branding for presentation datasets.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

public class CompanySeeder
{
    private readonly ICommandRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CompanySeeder(
        ICommandRepository<Company> repository,
        IUnitOfWork unitOfWork
        )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task GenerateDataAsync()
    {
        var entity = new Company
        {
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false,
            Name = "سوريا تيليكوم — بيئة العرض التجريبية",
            Currency = "SYP",
            Street = "كورنيش المزة — مجمع الاتصالات",
            City = "دمشق",
            State = "دمشق",
            ZipCode = "",
            Country = "سوريا",
            PhoneNumber = "011-0000000",
            FaxNumber = "011-0000001",
            EmailAddress = "info@syriatelecom-demo.local",
            Website = "https://syriatelecom-demo.local"
        };

        await _repository.CreateAsync(entity);
        await _unitOfWork.SaveAsync();

    }

}
