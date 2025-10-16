using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesQuotationManager;

public class SalesQuotationService
{
    private readonly ICommandRepository<SalesQuotation> _salesQuotationRepository;
    private readonly ICommandRepository<SalesQuotationItem> _salesQuotationItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SalesQuotationService(
        ICommandRepository<SalesQuotation> salesQuotationRepository,
        ICommandRepository<SalesQuotationItem> salesQuotationItemRepository,
        IUnitOfWork unitOfWork
        )
    {
        _salesQuotationRepository = salesQuotationRepository;
        _salesQuotationItemRepository = salesQuotationItemRepository;
        _unitOfWork = unitOfWork;
    }

    public void Recalculate(string salesQuotationId)
    {
        var salesQuotation = _salesQuotationRepository
            .GetQuery()
            .IsDeletedEqualTo()
            .Where(x => x.Id == salesQuotationId)
            .Include(x => x.Tax)
            .SingleOrDefault();

        if (salesQuotation == null)
            return;

        var salesQuotationItems = _salesQuotationItemRepository
            .GetQuery()
            .IsDeletedEqualTo()
            .Where(x => x.SalesQuotationId == salesQuotationId)
            .ToList();

        salesQuotation.BeforeTaxAmount = salesQuotationItems.Sum(x => x.Total ?? 0);

        var taxPercentage = salesQuotation.Tax?.Percentage ?? 0;
        salesQuotation.TaxAmount = (salesQuotation.BeforeTaxAmount ?? 0) * taxPercentage / 100;

        salesQuotation.AfterTaxAmount = (salesQuotation.BeforeTaxAmount ?? 0) + (salesQuotation.TaxAmount ?? 0);

        _salesQuotationRepository.Update(salesQuotation);
        _unitOfWork.Save();
    }
}
