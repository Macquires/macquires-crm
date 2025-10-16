using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.SeedManager.Demos;

public class LeadActivitySeeder
{
    private readonly ICommandRepository<LeadActivity> _leadActivityRepository;
    private readonly ICommandRepository<Lead> _leadRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public LeadActivitySeeder(
        ICommandRepository<LeadActivity> leadActivityRepository,
        ICommandRepository<Lead> leadRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _leadActivityRepository = leadActivityRepository;
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
            DateTime[] activityDates = GetRandomDays(date.Year, date.Month, 10);

            foreach (var activityDate in activityDates)
            {
                var leadId = GetRandomValue(leads, random);
                var fromDate = activityDate;
                var toDate = fromDate.AddHours(random.Next(1, 5));

                var leadActivity = new LeadActivity
                {
                    LeadId = leadId,
                    Number = _numberSequenceService.GenerateNumber(nameof(LeadActivity), "", "LA"),
                    Summary = $"Activity on {fromDate:MMMM d, yyyy}",
                    Description = $"Description for activity on {fromDate:MMMM d, yyyy}",
                    FromDate = fromDate,
                    ToDate = toDate,
                    Type = (LeadActivityType)random.Next(0, Enum.GetNames(typeof(LeadActivityType)).Length),
                    AttachmentName = random.Next(1, 100) % 2 == 0 ? $"file_{random.Next(1, 100)}.pdf" : null
                };

                await _leadActivityRepository.CreateAsync(leadActivity);
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