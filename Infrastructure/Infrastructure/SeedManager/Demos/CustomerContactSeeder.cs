// Demo-only — customer contacts for Syriatel demo subscribers.
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class CustomerContactSeeder
{
    private static readonly string[] ArabicFirstNames =
    [
        "آدم", "سارة", "مروان", "إيمان", "داوود", "جمانة",
        "كريم", "رشا", "جاسم", "ليلى", "فارس", "نادين"
    ];

    private static readonly string[] ArabicLastNames =
    [
        "الخطيب", "النابلسي", "الحمصي", "الحلبي", "الدمشقي", "اللاذقاني",
        "الحموي", "الطرطوسي", "السويداني", "الدرعي", "الفراتي", "الحسكي"
    ];

    private static readonly string[] JobTitles =
    [
        "مدير مبيعات", "مسؤول عقود", "مسؤول تقني", "مدير مالي", "مدير تشغيل",
        "مسؤول مشتريات", "مسؤول علاقات حكومية", "منسق مشاريع", "مسؤول دعم", "مدير فرع"
    ];

    private readonly ICommandRepository<CustomerContact> _customerContactRepository;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerContactSeeder(
        ICommandRepository<CustomerContact> customerContactRepository,
        ICommandRepository<Customer> customerRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _customerContactRepository = customerContactRepository;
        _customerRepository = customerRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var customerIds = await _customerRepository.GetQuery().Select(x => x.Id).ToListAsync();

        var customerContacts = new List<CustomerContact>();

        foreach (var customerId in customerIds)
        {
            for (int i = 0; i < 3; i++)
            {
                var firstName = GetRandomString(ArabicFirstNames, random);
                var lastName = GetRandomString(ArabicLastNames, random);
                var prefix = random.Next(2) == 0 ? "093" : "099";

                customerContacts.Add(new CustomerContact
                {
                    Name = $"{firstName} {lastName}",
                    Number = _numberSequenceService.GenerateNumber(nameof(CustomerContact), "", "CC"),
                    CustomerId = customerId,
                    JobTitle = GetRandomString(JobTitles, random),
                    EmailAddress = $"contact{random.Next(1000, 9999)}@syriatel-demo.local",
                    PhoneNumber = $"{prefix}{random.Next(1000000, 9999999)}"
                });
            }
        }

        foreach (var contact in customerContacts)
        {
            await _customerContactRepository.CreateAsync(contact);
        }

        await _unitOfWork.SaveAsync();
    }

    private static string GetRandomString(string[] array, Random random)
    {
        return array[random.Next(array.Length)];
    }
}
