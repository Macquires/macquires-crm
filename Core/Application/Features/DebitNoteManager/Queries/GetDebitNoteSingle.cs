using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DebitNoteManager.Queries;

public class GetDebitNoteSingleProfile : Profile
{
    public GetDebitNoteSingleProfile()
    {
    }
}

public class GetDebitNoteSingleResult
{
    public DebitNote? Data { get; init; }
    public List<InventoryTransaction>? TransactionList { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
}

public class GetDebitNoteSingleRequest : IRequest<GetDebitNoteSingleResult>
{
    public string? Id { get; init; }
}

public class GetDebitNoteSingleValidator : AbstractValidator<GetDebitNoteSingleRequest>
{
    public GetDebitNoteSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetDebitNoteSingleHandler : IRequestHandler<GetDebitNoteSingleRequest, GetDebitNoteSingleResult>
{
    private readonly IQueryContext _context;

    public GetDebitNoteSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetDebitNoteSingleResult> Handle(GetDebitNoteSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .DebitNote
            .AsNoTracking()
            .Include(x => x.PurchaseReturn)
                .ThenInclude(x => x.GoodsReceive)
                    .ThenInclude(x => x.PurchaseOrder)
                        .ThenInclude(x => x.Vendor)
            .Include(x => x.PurchaseReturn)
                .ThenInclude(x => x.GoodsReceive)
                    .ThenInclude(x => x.PurchaseOrder)
                        .ThenInclude(x => x.Tax)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);


        var queryTransactionList = _context
            .InventoryTransaction
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Include(x => x.WarehouseFrom)
            .Include(x => x.WarehouseTo)
            .Where(x => x.ModuleId == entity.PurchaseReturn.Id && x.ModuleName == nameof(PurchaseReturn))
            .AsQueryable();

        var transactionList = await queryTransactionList.ToListAsync(cancellationToken);

        var beforeTaxAmount = transactionList.Sum(x => x.Product?.UnitPrice * x.Movement);
        var taxAmount = entity?.PurchaseReturn?.GoodsReceive?.PurchaseOrder?.Tax?.Percentage * beforeTaxAmount / 100;
        var afterTaxAmount = beforeTaxAmount + taxAmount;

        return new GetDebitNoteSingleResult
        {
            Data = entity,
            TransactionList = transactionList,
            BeforeTaxAmount = beforeTaxAmount,
            TaxAmount = taxAmount,
            AfterTaxAmount = afterTaxAmount
        };
    }
}