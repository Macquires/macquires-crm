// Demo-only — Syria Telecom retail & B2B subscribers (fictional MSISDNs).
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class CustomerSeeder
{
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<CustomerGroup> _groupRepository;
    private readonly ICommandRepository<CustomerCategory> _categoryRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerSeeder(
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<CustomerGroup> groupRepository,
        ICommandRepository<CustomerCategory> categoryRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _customerRepository = customerRepository;
        _groupRepository = groupRepository;
        _categoryRepository = categoryRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var groups = (await _groupRepository.GetQuery().ToListAsync()).Select(x => x.Id).ToArray();
        var categories = (await _categoryRepository.GetQuery().ToListAsync()).Select(x => x.Id).ToArray();

        var cityRows = new (string City, string Street, string State)[]
        {
            ("دمشق", "شارع الحجاز", "دمشق"),
            ("دمشق", "ضاحية قدسيا", "ريف دمشق"),
            ("حلب", "حي العزيزية", "حلب"),
            ("حمص", "شارع المحطة", "حمص"),
            ("اللاذقية", "المشروع السابع", "اللاذقية"),
            ("حماة", "طريق حمص", "حماة")
        };

        var random = new Random();

        var customers = new List<Customer>
        {
            new Customer { Name = "محمد علي" },
            new Customer { Name = "ريما الخطيب" },
            new Customer { Name = "شركة المتحدون للاستيراد" },
            new Customer { Name = "ليان حسن" },
            new Customer { Name = "عمر الدرويش" },
            new Customer { Name = "نورا صالح" },
            new Customer { Name = "مؤسسة النور للاتصالات" },
            new Customer { Name = "خالد منصور" },
            new Customer { Name = "سارة يوسف" },
            new Customer { Name = "فادي الأسعد" },
            new Customer { Name = "مجموعة الفردوس التجارية" },
            new Customer { Name = "هند المالكي" },
            new Customer { Name = "ياسر عوض" },
            new Customer { Name = "ميساء الحموي" },
            new Customer { Name = "تقنيات الشام للخدمات" },
            new Customer { Name = "باسل مراد" },
            new Customer { Name = "دانيا إبراهيم" },
            new Customer { Name = "زياد القاسم" },
            new Customer { Name = "شركة اليرموك للتوزيع" },
            new Customer { Name = "غادة نعمة" }
        };

        foreach (var customer in customers)
        {
            customer.Number = _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST");
            customer.CustomerGroupId = GetRandomValue(groups, random);
            customer.CustomerCategoryId = GetRandomValue(categories, random);

            var row = cityRows[random.Next(cityRows.Length)];
            customer.City = row.City;
            customer.Street = row.Street;
            customer.State = row.State;
            customer.ZipCode = $"{1000 + random.Next(9000)}";

            var prefix = random.Next(2) == 0 ? "093" : "099";
            customer.PhoneNumber = $"{prefix}{random.Next(1000000, 9999999)}";

            customer.Country = "سوريا";
            customer.Description = "مشترك ضمن بيئة العرض التجريبية لسوريا تيليكوم.";
            customer.Website = "https://syriatelecom-demo.local";

            customer.EmailAddress = $"subscriber{random.Next(10000, 999999)}@syriatelecom-demo.local";

            await _customerRepository.CreateAsync(customer);
        }

        await _unitOfWork.SaveAsync();
    }

    private static T GetRandomValue<T>(T[] array, Random random)
    {
        return array[random.Next(array.Length)];
    }
}
