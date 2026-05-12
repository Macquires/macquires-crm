// Demo-only — lead contacts aligned with Syria Telecom demo geography.
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class LeadContactSeeder
{
    private static readonly string[] ArabicNames =
    [
        "ليان أحمد", "سامر الخوري", "مها الزعبي", "كريم حداد", "رغد المصري",
        "طارق بيطار", "هند العلي", "باسل مراد", "نور الدين", "سلمى يوسف"
    ];

    private readonly ICommandRepository<LeadContact> _leadContactRepository;
    private readonly ICommandRepository<Lead> _leadRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public LeadContactSeeder(
        ICommandRepository<LeadContact> leadContactRepository,
        ICommandRepository<Lead> leadRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _leadContactRepository = leadContactRepository;
        _leadRepository = leadRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-11).Year, dateFinish.AddMonths(-11).Month, 1);
        var leads = await _leadRepository.GetQuery().Select(l => l.Id).ToListAsync();

        for (DateTime date = dateStart; date <= dateFinish; date = date.AddMonths(1))
        {
            DateTime[] contactDates = GetRandomDays(date.Year, date.Month, 5, random);

            foreach (var contactDate in contactDates)
            {
                var leadId = GetRandomValue(leads, random);
                var fullName = ArabicNames[random.Next(ArabicNames.Length)];
                var prefix = random.Next(2) == 0 ? "093" : "099";
                var mobile = $"{prefix}{random.Next(1000000, 9999999)}";

                var leadContact = new LeadContact
                {
                    LeadId = leadId,
                    Number = _numberSequenceService.GenerateNumber(nameof(LeadContact), "", "LC"),
                    FullName = fullName,
                    Description = $"جهة اتصال للمتابعة — فرصة سوريا تيليكوم — تاريخ {contactDate:yyyy-MM-dd}.",
                    AddressStreet = "شارع بغداد — بناء الخدمات",
                    AddressCity = "دمشق",
                    AddressState = "دمشق",
                    AddressZipCode = "0000",
                    AddressCountry = "سوريا",
                    PhoneNumber = $"011{random.Next(1000000, 9999999)}",
                    FaxNumber = $"011{random.Next(1000000, 9999999)}",
                    MobileNumber = mobile,
                    Email = $"contact{random.Next(100, 999)}@syriatelecom-lead.demo",
                    Website = "https://syriatelecom-demo.local",
                    WhatsApp = mobile,
                    LinkedIn = "linkedin.com/syriatelecom-demo",
                    Facebook = "facebook.com/syriatelecom-demo",
                    Twitter = "twitter.com/syriatelecom-demo",
                    Instagram = "instagram.com/syriatelecom-demo",
                    AvatarName = $"avatar_{random.Next(1, 100)}.jpg"
                };

                await _leadContactRepository.CreateAsync(leadContact);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private static string GetRandomValue(List<string> list, Random random)
    {
        return list[random.Next(list.Count)];
    }

    private static DateTime[] GetRandomDays(int year, int month, int count, Random random)
    {
        var daysInMonth = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).ToList();
        var selectedDays = new List<int>();

        for (int i = 0; i < count && daysInMonth.Count > 0; i++)
        {
            int day = daysInMonth[random.Next(daysInMonth.Count)];
            selectedDays.Add(day);
            daysInMonth.Remove(day);
        }

        return selectedDays.Select(day => new DateTime(year, month, day)).ToArray();
    }
}
