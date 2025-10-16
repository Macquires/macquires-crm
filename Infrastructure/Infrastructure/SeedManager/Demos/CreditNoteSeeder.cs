using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class CreditNoteSeeder
{
    private readonly ICommandRepository<CreditNote> _creditNoteRepository;
    private readonly ICommandRepository<SalesReturn> _salesReturnRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public CreditNoteSeeder(
        ICommandRepository<CreditNote> creditNoteRepository,
        ICommandRepository<SalesReturn> salesReturnRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _creditNoteRepository = creditNoteRepository;
        _salesReturnRepository = salesReturnRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-12).Year, dateFinish.AddMonths(-12).Month, 1);
        var confirmedSalesReturns = new List<string>(await _salesReturnRepository.GetQuery()
            .Where(sr => sr.Status == SalesReturnStatus.Confirmed)
            .Select(sr => sr.Id)
            .ToListAsync());

        for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
        {
            DateTime[] creditNoteDates = GetRandomDays(date.Year, date.Month, 3);

            foreach (var creditNoteDate in creditNoteDates)
            {
                var status = GetRandomStatus(random);

                if (confirmedSalesReturns.Count == 0) break;

                var salesReturnId = GetRandomAndRemove(confirmedSalesReturns, random);

                var creditNote = new CreditNote
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(CreditNote), "", "CN"),
                    CreditNoteDate = creditNoteDate,
                    CreditNoteStatus = status,
                    Description = $"Credit Note for {creditNoteDate:MMMM yyyy}",
                    SalesReturnId = salesReturnId
                };

                await _creditNoteRepository.CreateAsync(creditNote);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private CreditNoteStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { CreditNoteStatus.Draft, CreditNoteStatus.Cancelled, CreditNoteStatus.Confirmed, CreditNoteStatus.Archived };
        var weights = new[] { 1, 1, 4, 1 };

        int totalWeight = weights.Sum();
        int randomNumber = random.Next(0, totalWeight);

        for (int i = 0; i < statuses.Length; i++)
        {
            if (randomNumber < weights[i])
            {
                return statuses[i];
            }
            randomNumber -= weights[i];
        }

        return CreditNoteStatus.Confirmed;
    }

    private static string GetRandomAndRemove(List<string> list, Random random)
    {
        int index = random.Next(list.Count);
        string value = list[index];
        list.RemoveAt(index);
        return value;
    }

    private static DateTime[] GetRandomDays(int year, int month, int count)
    {
        var random = new Random();
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