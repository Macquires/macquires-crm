using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class InvoiceSeeder
{
    private readonly ICommandRepository<Invoice> _invoiceRepository;
    private readonly ICommandRepository<SalesOrder> _salesOrderRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceSeeder(
        ICommandRepository<Invoice> invoiceRepository,
        ICommandRepository<SalesOrder> salesOrderRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _invoiceRepository = invoiceRepository;
        _salesOrderRepository = salesOrderRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-12).Year, dateFinish.AddMonths(-12).Month, 1);
        var confirmedSalesOrders = new List<string>(await _salesOrderRepository.GetQuery()
            .Where(so => so.OrderStatus == SalesOrderStatus.Confirmed)
            .Select(so => so.Id)
            .ToListAsync());

        for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
        {
            DateTime[] invoiceDates = GetRandomDays(date.Year, date.Month, 5);

            foreach (var invoiceDate in invoiceDates)
            {
                var status = GetRandomStatus(random);

                if (confirmedSalesOrders.Count == 0) break;

                var salesOrderId = GetRandomAndRemove(confirmedSalesOrders, random);

                var invoice = new Invoice
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(Invoice), "", "INV"),
                    InvoiceDate = invoiceDate,
                    InvoiceStatus = status,
                    Description = $"Invoice for {invoiceDate:MMMM yyyy}",
                    SalesOrderId = salesOrderId
                };

                await _invoiceRepository.CreateAsync(invoice);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private InvoiceStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { InvoiceStatus.Draft, InvoiceStatus.Cancelled, InvoiceStatus.Confirmed };
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

        return InvoiceStatus.Confirmed;
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