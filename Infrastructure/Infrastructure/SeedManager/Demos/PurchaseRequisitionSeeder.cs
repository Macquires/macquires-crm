using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Application.Features.PurchaseRequisitionManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

public class PurchaseRequisitionSeeder
{
    private readonly PurchaseRequisitionService _purchaseRequisitionService;
    private readonly ICommandRepository<PurchaseRequisition> _purchaseRequisitionRepository;
    private readonly ICommandRepository<PurchaseRequisitionItem> _purchaseRequisitionItemRepository;
    private readonly ICommandRepository<Vendor> _vendorRepository;
    private readonly ICommandRepository<Tax> _taxRepository;
    private readonly ICommandRepository<Product> _productRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseRequisitionSeeder(
        PurchaseRequisitionService purchaseRequisitionService,
        ICommandRepository<PurchaseRequisition> purchaseRequisitionRepository,
        ICommandRepository<PurchaseRequisitionItem> purchaseRequisitionItemRepository,
        ICommandRepository<Vendor> vendorRepository,
        ICommandRepository<Tax> taxRepository,
        ICommandRepository<Product> productRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork
    )
    {
        _purchaseRequisitionService = purchaseRequisitionService;
        _purchaseRequisitionRepository = purchaseRequisitionRepository;
        _purchaseRequisitionItemRepository = purchaseRequisitionItemRepository;
        _vendorRepository = vendorRepository;
        _taxRepository = taxRepository;
        _productRepository = productRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var random = new Random();
        var vendors = await _vendorRepository.GetQuery().Select(x => x.Id).ToListAsync();
        var taxes = await _taxRepository.GetQuery().Select(x => x.Id).ToListAsync();
        var products = await _productRepository.GetQuery().ToListAsync();

        var dateFinish = DateTime.Now;
        var dateStart = new DateTime(dateFinish.AddMonths(-11).Year, dateFinish.AddMonths(-11).Month, 1);

        for (DateTime date = dateStart; date <= dateFinish; date = date.AddMonths(1))
        {
            DateTime[] requisitionDates = GetRandomDays(date.Year, date.Month, 4);

            foreach (var requisitionDate in requisitionDates)
            {
                var status = GetRandomStatus(random);
                var purchaseRequisition = new PurchaseRequisition
                {
                    Number = _numberSequenceService.GenerateNumber(nameof(PurchaseRequisition), "", "PR"),
                    RequisitionDate = requisitionDate,
                    RequisitionStatus = status,
                    Description = $"Requisition for {requisitionDate:MMMM yyyy}",
                    VendorId = GetRandomValue(vendors, random),
                    TaxId = GetRandomValue(taxes, random),
                };
                await _purchaseRequisitionRepository.CreateAsync(purchaseRequisition);

                int numberOfItems = random.Next(2, 5);
                for (int i = 0; i < numberOfItems; i++)
                {
                    var qty = random.Next(2, 5);
                    var product = products[random.Next(products.Count)];
                    var purchaseRequisitionItem = new PurchaseRequisitionItem
                    {
                        PurchaseRequisitionId = purchaseRequisition.Id,
                        ProductId = product.Id,
                        Summary = product.Number,
                        UnitPrice = product.UnitPrice,
                        Quantity = qty,
                        Total = product.UnitPrice * qty
                    };
                    await _purchaseRequisitionItemRepository.CreateAsync(purchaseRequisitionItem);
                }

                await _unitOfWork.SaveAsync();

                _purchaseRequisitionService.Recalculate(purchaseRequisition.Id);
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private PurchaseRequisitionStatus GetRandomStatus(Random random)
    {
        var statuses = new[] { PurchaseRequisitionStatus.Draft, PurchaseRequisitionStatus.Cancelled, PurchaseRequisitionStatus.Confirmed, PurchaseRequisitionStatus.Ordered };
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

        return PurchaseRequisitionStatus.Confirmed;
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