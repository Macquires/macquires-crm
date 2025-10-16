using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class LeadContactSeeder
{
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
            DateTime[] contactDates = GetRandomDays(date.Year, date.Month, 5);

            foreach (var contactDate in contactDates)
            {
                var leadId = GetRandomValue(leads, random);
                var leadContact = new LeadContact
                {
                    LeadId = leadId,
                    Number = _numberSequenceService.GenerateNumber(nameof(LeadContact), "", "LC"),
                    FullName = $"Contact {random.Next(1000, 9999)}",
                    Description = "Sample contact description",
                    AddressStreet = "456 Elm St",
                    AddressCity = "Anytown",
                    AddressState = "State",
                    AddressZipCode = "67890",
                    AddressCountry = "Country",
                    PhoneNumber = $"+1{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    FaxNumber = $"+1{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    MobileNumber = $"+1{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    Email = $"contact{random.Next(1000, 9999)}@company.com",
                    Website = $"www.contact{random.Next(1000, 9999)}.com",
                    WhatsApp = $"+1{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                    LinkedIn = $"linkedin.com/in/contact{random.Next(1000, 9999)}",
                    Facebook = $"facebook.com/contact{random.Next(1000, 9999)}",
                    Twitter = $"twitter.com/contact{random.Next(1000, 9999)}",
                    Instagram = $"instagram.com/contact{random.Next(1000, 9999)}",
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

    private static DateTime[] GetRandomDays(int year, int month, int count)
    {
        var random = new Random();
        var daysInMonth = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).ToList();
        var selectedDays = new List<int>();

        for (int i = 0; i < count; i++)
        {
            int day = daysInMonth[random.Next(daysInMonth.Count)];
            selectedDays.Add(day);
            daysInMonth.Remove(day);
        }

        return selectedDays.Select(day => new DateTime(year, month, day)).ToArray();
    }
}