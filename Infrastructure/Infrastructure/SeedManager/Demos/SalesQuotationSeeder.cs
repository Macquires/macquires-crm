using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Application.Features.SalesQuotationManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class SalesQuotationSeeder
{
    private readonly SalesQuotationService _salesQuotationService;
    private readonly ICommandRepository<SalesQuotation> _salesQuotationRepository;
    private readonly ICommandRepository<SalesQuotationItem> _salesQuotationItemRepository;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<Tax> _taxRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public SalesQuotationSeeder(
        SalesQuotationService salesQuotationService,
        ICommandRepository<SalesQuotation> salesQuotationRepository,
        ICommandRepository<SalesQuotationItem> salesQuotationItemRepository,
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<Tax> taxRepository,
        ICommandRepository<Product> productRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _salesQuotationService = salesQuotationService;
        _salesQuotationRepository = salesQuotationRepository;
        _salesQuotationItemRepository = salesQuotationItemRepository;
        _customerRepository = customerRepository;
        _taxRepository = taxRepository;
        _productRepository = productRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var customers = await _customerRepository.GetQuery().Select(x => x.Id).ToListAsync();
        var taxes = await _taxRepository.GetQuery().Select(x => x.Id).ToListAsync();
        var products = await _productRepository.GetQuery().ToListAsync();

        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-11).Year, dateFinish.AddMonths(-11).Month, 1);

        for (DateTime date = dateStart; date <= dateFinish; date = date.AddMonths(1))
        {
            DateTime[] quotationDates = GetRandomDays(date.Year, date.Month, 4);

            foreach (var quotationDate in quotationDates)
            {
                var status = GetRandomStatus(random);
                var salesQuotation = new SalesQuotation
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(SalesQuotation), "", "SQ"),
                    QuotationDate = quotationDate,
                    QuotationStatus = status,
                    Description = $"Quotation for {quotationDate:MMMM yyyy}",
                    CustomerId = GetRandomValue(customers, random),
                    TaxId = GetRandomValue(taxes, random),
                };
                await _salesQuotationRepository.CreateAsync(salesQuotation);

                int numberOfItems = random.Next(2, 5);
                for (int i = 0; i < numberOfItems; i++)
                {
                    var qty = random.Next(2, 5);
                    var product = products[random.Next(products.Count)];
                    var salesQuotationItem = new SalesQuotationItem
                    {
                        SalesQuotationId = salesQuotation.Id,
                        ProductId = product.Id,
                        Summary = product.Number,
                        UnitPrice = product.UnitPrice,
                        Quantity = qty,
                        Total = product.UnitPrice * qty
                    };
                    await _salesQuotationItemRepository.CreateAsync(salesQuotationItem);
                }

                await _unitOfWork.SaveAsync();

                _salesQuotationService.Recalculate(salesQuotation.Id);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private SalesQuotationStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { SalesQuotationStatus.Draft, SalesQuotationStatus.Cancelled, SalesQuotationStatus.Confirmed, SalesQuotationStatus.Ordered };
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

        return SalesQuotationStatus.Confirmed;
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