using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class DebitNoteSeeder
{
    private readonly ICommandRepository<DebitNote> _debitNoteRepository;
    private readonly ICommandRepository<PurchaseReturn> _purchaseReturnRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public DebitNoteSeeder(
        ICommandRepository<DebitNote> debitNoteRepository,
        ICommandRepository<PurchaseReturn> purchaseReturnRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _debitNoteRepository = debitNoteRepository;
        _purchaseReturnRepository = purchaseReturnRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-12).Year, dateFinish.AddMonths(-12).Month, 1);
        var confirmedPurchaseReturns = new List<string>(await _purchaseReturnRepository.GetQuery()
            .Where(pr => pr.Status == PurchaseReturnStatus.Confirmed)
            .Select(pr => pr.Id)
            .ToListAsync());

        for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
        {
            DateTime[] debitNoteDates = GetRandomDays(date.Year, date.Month, 3);

            foreach (var debitNoteDate in debitNoteDates)
            {
                var status = GetRandomStatus(random);

                if (confirmedPurchaseReturns.Count == 0) break;

                var purchaseReturnId = GetRandomAndRemove(confirmedPurchaseReturns, random);

                var debitNote = new DebitNote
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(DebitNote), "", "DN"),
                    DebitNoteDate = debitNoteDate,
                    DebitNoteStatus = status,
                    Description = $"Debit Note for {debitNoteDate:MMMM yyyy}",
                    PurchaseReturnId = purchaseReturnId
                };

                await _debitNoteRepository.CreateAsync(debitNote);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private DebitNoteStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { DebitNoteStatus.Draft, DebitNoteStatus.Cancelled, DebitNoteStatus.Confirmed, DebitNoteStatus.Archived };
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

        return DebitNoteStatus.Confirmed;
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