using Application.Common.Repositories;
using Application.Common.Security;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class CustomerSeeder
{
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<CustomerGroup> _groupRepository;
    private readonly ICommandRepository<CustomerCategory> _categoryRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DataContext _context;
    private readonly IFieldEncryptionService _encryption;

    public CustomerSeeder(
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<CustomerGroup> groupRepository,
        ICommandRepository<CustomerCategory> categoryRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork,
        DataContext context,
        IFieldEncryptionService encryption)
    {
        _customerRepository = customerRepository;
        _groupRepository = groupRepository;
        _categoryRepository = categoryRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
        _context = context;
        _encryption = encryption;
    }

    public async Task GenerateDataAsync()
    {
        var groups = (await _groupRepository.GetQuery().ToListAsync()).Select(x => x.Id).ToArray();
        var categories = (await _categoryRepository.GetQuery().ToListAsync()).Select(x => x.Id).ToArray();
        var random = new Random();

        var branches = await _context.OrgUnit
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Branch)
            .ToListAsync();

        var defaultBranchId = branches.FirstOrDefault(b => b.NameAr.Contains("المزة", StringComparison.Ordinal))?.Id
            ?? branches.FirstOrDefault()?.Id;

        var names = new[]
        {
            ("محمد علي", false, "0101234567", "دمشق"),
            ("ريما الخطيب", false, "0102345678", "حلب"),
            ("شركة المتحدون للاستيراد", true, "CR-100200", "دمشق"),
            ("ليان حسن", false, "0103456789", "طرطوس"),
            ("مؤسسة النور للاتصالات", true, "CR-200300", "اللاذقية"),
        };

        foreach (var (name, corporate, idKey, city) in names)
        {
            var address = new PostalAddress("شارع الحجاز", city, city, "10001", "سوريا");
            var account = await _numberSequenceService.GenerateNumberAsync(nameof(Customer), "", "CST");
            var phone = $"093{random.Next(1000000, 9999999)}";
            var email = $"subscriber{random.Next(10000, 999999)}@syriatel-demo.local";
            var groupId = groups[random.Next(groups.Length)];
            var catId = categories[random.Next(categories.Length)];

            Customer entity = corporate
                ? CorporateCustomer.Create(name, account, idKey, address, email, phone, groupId, catId,
                    taxNumber: "TAX-99001", authorizedSignatoryName: "مفوض معتمد")
                : IndividualCustomer.Create(name, account, idKey, address, email, phone, groupId, catId);

            if (entity is IndividualCustomer individual)
            {
                individual.SyncNationalIdSearchHash(_encryption);
            }

            var branchId = branches.FirstOrDefault(b => b.NameAr.Contains(city, StringComparison.Ordinal))?.Id
                ?? defaultBranchId;
            if (!string.IsNullOrEmpty(branchId))
            {
                entity.SetOrgUnitId(branchId);
            }

            await _customerRepository.CreateAsync(entity);
        }

        await _unitOfWork.SaveAsync();
    }
}
