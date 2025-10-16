using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class PaymentDisburseSeeder
{
    private readonly ICommandRepository<PaymentDisburse> _paymentDisburseRepository;
    private readonly ICommandRepository<Bill> _billRepository;
    private readonly ICommandRepository<PaymentMethod> _paymentMethodRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentDisburseSeeder(
        ICommandRepository<PaymentDisburse> paymentDisburseRepository,
        ICommandRepository<Bill> billRepository,
        ICommandRepository<PaymentMethod> paymentMethodRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _paymentDisburseRepository = paymentDisburseRepository;
        _billRepository = billRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-12).Year, dateFinish.AddMonths(-12).Month, 1);
        var confirmedBills = new List<Bill>(await _billRepository.GetQuery()
            .Where(b => b.BillStatus == BillStatus.Confirmed)
            .ToListAsync());
        var paymentMethods = await _paymentMethodRepository.GetQuery().Select(pm => pm.Id).ToListAsync();

        for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
        {
            DateTime[] paymentDates = GetRandomDays(date.Year, date.Month, 4);

            foreach (var paymentDate in paymentDates)
            {
                var status = GetRandomStatus(random);

                if (confirmedBills.Count == 0) break;

                var bill = GetRandomAndRemove(confirmedBills, random);
                var paymentMethodId = GetRandomValue(paymentMethods, random);

                var paymentDisburse = new PaymentDisburse
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(PaymentDisburse), "", "PYDS"),
                    Description = $"Payment Disbursed on {paymentDate:MMMM yyyy}",
                    PaymentDate = paymentDate,
                    PaymentMethodId = paymentMethodId,
                    PaymentAmount = bill.PurchaseOrder?.AfterTaxAmount,
                    Status = status,
                    BillId = bill.Id
                };

                await _paymentDisburseRepository.CreateAsync(paymentDisburse);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private PaymentDisburseStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { PaymentDisburseStatus.Draft, PaymentDisburseStatus.Cancelled, PaymentDisburseStatus.Confirmed, PaymentDisburseStatus.Archived };
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

        return PaymentDisburseStatus.Confirmed;
    }

    private static T GetRandomAndRemove<T>(List<T> list, Random random)
    {
        int index = random.Next(list.Count);
        T value = list[index];
        list.RemoveAt(index);
        return value;
    }

    private static T GetRandomValue<T>(List<T> list, Random random)
    {
        return list[random.Next(list.Count)];
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