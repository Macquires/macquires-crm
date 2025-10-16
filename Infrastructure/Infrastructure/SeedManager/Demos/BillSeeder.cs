using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class BillSeeder
{
    private readonly ICommandRepository<Bill> _billRepository;
    private readonly ICommandRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public BillSeeder(
        ICommandRepository<Bill> billRepository,
        ICommandRepository<PurchaseOrder> purchaseOrderRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _billRepository = billRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-12).Year, dateFinish.AddMonths(-12).Month, 1);
        var confirmedPurchaseOrders = new List<string>(await _purchaseOrderRepository.GetQuery()
            .Where(po => po.OrderStatus == PurchaseOrderStatus.Confirmed)
            .Select(po => po.Id)
            .ToListAsync());

        for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
        {
            DateTime[] billDates = GetRandomDays(date.Year, date.Month, 5);

            foreach (var billDate in billDates)
            {
                var status = GetRandomStatus(random);
                if (confirmedPurchaseOrders.Count == 0)
                {
                    break;
                }

                var purchaseOrderId = GetRandomAndRemove(confirmedPurchaseOrders, random);

                var bill = new Bill
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(Bill), "", "BIL"),
                    BillDate = billDate,
                    BillStatus = status,
                    Description = $"Bill for {billDate:MMMM yyyy}",
                    PurchaseOrderId = purchaseOrderId
                };

                await _billRepository.CreateAsync(bill);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private BillStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { BillStatus.Draft, BillStatus.Cancelled, BillStatus.Confirmed };
        var weights = new[] { 1, 1, 4 };

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

        return BillStatus.Confirmed;
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