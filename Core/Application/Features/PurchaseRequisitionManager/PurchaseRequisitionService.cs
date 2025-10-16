using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseRequisitionManager;

public class PurchaseRequisitionService
{
    private readonly ICommandRepository<PurchaseRequisition> _purchaseRequisitionRepository;
    private readonly ICommandRepository<PurchaseRequisitionItem> _purchaseRequisitionItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseRequisitionService(
        ICommandRepository<PurchaseRequisition> purchaseRequisitionRepository,
        ICommandRepository<PurchaseRequisitionItem> purchaseRequisitionItemRepository,
        IUnitOfWork unitOfWork
        )
    {
        _purchaseRequisitionRepository = purchaseRequisitionRepository;
        _purchaseRequisitionItemRepository = purchaseRequisitionItemRepository;
        _unitOfWork = unitOfWork;
    }

    public void Recalculate(string purchaseRequisitionId)
    {
        var purchaseRequisition = _purchaseRequisitionRepository
            .GetQuery()
            .IsDeletedEqualTo()
            .Where(x => x.Id == purchaseRequisitionId)
            .Include(x => x.Tax)
            .SingleOrDefault();

        if (purchaseRequisition == null)
            return;

        var purchaseRequisitionItems = _purchaseRequisitionItemRepository
            .GetQuery()
            .IsDeletedEqualTo()
            .Where(x => x.PurchaseRequisitionId == purchaseRequisitionId)
            .ToList();

        purchaseRequisition.BeforeTaxAmount = purchaseRequisitionItems.Sum(x => x.Total ?? 0);

        var taxPercentage = purchaseRequisition.Tax?.Percentage ?? 0;
        purchaseRequisition.TaxAmount = (purchaseRequisition.BeforeTaxAmount ?? 0) * taxPercentage / 100;

        purchaseRequisition.AfterTaxAmount = (purchaseRequisition.BeforeTaxAmount ?? 0) + (purchaseRequisition.TaxAmount ?? 0);

        _purchaseRequisitionRepository.Update(purchaseRequisition);
        _unitOfWork.Save();
    }
}
