using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesReturnManager.Queries;


public class GetSalesReturnSingleProfile : Profile
{
    public GetSalesReturnSingleProfile()
    {
    }
}

public class GetSalesReturnSingleResult
{
    public SalesReturn? Data { get; init; }
    public List<InventoryTransaction>? TransactionList { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
}

public class GetSalesReturnSingleRequest : IRequest<GetSalesReturnSingleResult>
{
    public string? Id { get; init; }
}

public class GetSalesReturnSingleValidator : AbstractValidator<GetSalesReturnSingleRequest>
{
    public GetSalesReturnSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetSalesReturnSingleHandler : IRequestHandler<GetSalesReturnSingleRequest, GetSalesReturnSingleResult>
{
    private readonly IQueryContext _context;

    public GetSalesReturnSingleHandler(
        IQueryContext context
        )
    {
        _context = context;
    }

    public async Task<GetSalesReturnSingleResult> Handle(GetSalesReturnSingleRequest request, CancellationToken cancellationToken)
    {
        var queryData = _context
            .SalesReturn
            .AsNoTracking()
            .Include(x => x.DeliveryOrder)
                .ThenInclude(x => x.SalesOrder)
                    .ThenInclude(x => x.Customer)
            .Include(x => x.DeliveryOrder)
                .ThenInclude(x => x.SalesOrder)
                    .ThenInclude(x => x.Tax)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var data = await queryData.SingleOrDefaultAsync(cancellationToken);


        var queryTransactionList = _context
            .InventoryTransaction
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Include(x => x.WarehouseFrom)
            .Include(x => x.WarehouseTo)
            .Where(x => x.ModuleId == request.Id && x.ModuleName == nameof(SalesReturn))
            .AsQueryable();

        var transactionList = await queryTransactionList.ToListAsync(cancellationToken);

        var beforeTaxAmount = transactionList.Sum(x => x.Product?.UnitPrice * x.Movement);
        var taxAmount = data?.DeliveryOrder?.SalesOrder?.Tax?.Percentage * beforeTaxAmount / 100;
        var afterTaxAmount = beforeTaxAmount + taxAmount;

        return new GetSalesReturnSingleResult
        {
            Data = data,
            TransactionList = transactionList,
            BeforeTaxAmount = beforeTaxAmount,
            TaxAmount = taxAmount,
            AfterTaxAmount = afterTaxAmount
        };
    }
}