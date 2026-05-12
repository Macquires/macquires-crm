// Demo-only sales history including fictional MGR/TKO reference numbers (not production billing).
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Application.Features.SalesOrderManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class SalesOrderSeeder
{
    private readonly SalesOrderService _salesOrderService;
    private readonly ICommandRepository<SalesOrder> _salesOrderRepository;
    private readonly ICommandRepository<SalesOrderItem> _salesOrderItemRepository;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<Tax> _taxRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public SalesOrderSeeder(
        SalesOrderService salesOrderService,
        ICommandRepository<SalesOrder> salesOrderRepository,
        ICommandRepository<SalesOrderItem> salesOrderItemRepository,
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<Tax> taxRepository,
        ICommandRepository<Product> productRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _salesOrderService = salesOrderService;
        _salesOrderRepository = salesOrderRepository;
        _salesOrderItemRepository = salesOrderItemRepository;
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

        var physicalProducts = products.Where(p => p.Physical == true).ToList();
        var digitalProducts = products.Where(p => p.Physical == false).ToList();

        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-12).Year, dateFinish.AddMonths(-12).Month, 1);

        for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
        {
            DateTime[] transactionDates = GetRandomDays(date.Year, date.Month, 6);

            foreach (DateTime transDate in transactionDates)
            {
                var salesOrder = new SalesOrder
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(SalesOrder), "", "SO"),
                    OrderDate = transDate,
                    OrderStatus = GetRandomStatus(random),
                    CustomerId = GetRandomValue(customers, random),
                    TaxId = GetRandomValue(taxes, random),
                };

                if (random.NextDouble() < 0.22)
                {
                    salesOrder.Description = BuildTelecomScenarioDescription(random);
                }

                await _salesOrderRepository.CreateAsync(salesOrder);

                int numberOfProducts = random.Next(3, 6);
                var crossSellBundle = random.NextDouble() < 0.35 && digitalProducts.Count > 0 && physicalProducts.Count > 0;

                if (crossSellBundle)
                {
                    var d = digitalProducts[random.Next(digitalProducts.Count)];
                    await AddLineAsync(salesOrder.Id, d, random);

                    var p = physicalProducts[random.Next(physicalProducts.Count)];
                    await AddLineAsync(salesOrder.Id, p, random);

                    numberOfProducts -= 2;
                }

                for (int i = 0; i < numberOfProducts; i++)
                {
                    var product = products[random.Next(products.Count)];
                    await AddLineAsync(salesOrder.Id, product, random);
                }

                await _unitOfWork.SaveAsync();

                _salesOrderService.Recalculate(salesOrder.Id);
            }
        }
    }

    private string BuildTelecomScenarioDescription(Random random)
    {
        var scenario = random.Next(0, 3);
        return scenario switch
        {
            0 => $"تحويل باقة: من مسبق الدفع إلى فاتورة — مرجع {_numberSequenceService.GenerateNumber("TelecomMigrationDemo", "MGR-", "", useDate: false)} — طلب عبر مركز سوريا تيليكوم دمشق — الحجاز",
            1 => $"تبديل شريحة (SIM Swap) — مرجع {_numberSequenceService.GenerateNumber("TelecomMigrationDemo", "MGR-", "", useDate: false)} — تحقق من الهوية",
            _ => $"نقل ملكية خط (Take Over) — مرجع {_numberSequenceService.GenerateNumber("TelecomTakeOverDemo", "TKO-", "", useDate: false)} — موافقة الطرفين",
        };
    }

    private async Task AddLineAsync(string salesOrderId, Product product, Random random)
    {
        var qty = random.Next(1, 4);
        var salesOrderItem = new SalesOrderItem
        {
            SalesOrderId = salesOrderId,
            ProductId = product.Id,
            Summary = product.Number,
            UnitPrice = product.UnitPrice,
            Quantity = qty,
            Total = product.UnitPrice * qty
        };
        await _salesOrderItemRepository.CreateAsync(salesOrderItem);
    }

    private SalesOrderStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { SalesOrderStatus.Draft, SalesOrderStatus.Cancelled, SalesOrderStatus.Confirmed, SalesOrderStatus.Archived };
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

        return SalesOrderStatus.Confirmed;
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
