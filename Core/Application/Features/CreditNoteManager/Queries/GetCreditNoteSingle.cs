using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CreditNoteManager.Queries;

public class GetCreditNoteSingleProfile : Profile
{
    public GetCreditNoteSingleProfile()
    {
    }
}

public class GetCreditNoteSingleResult
{
    public CreditNote? Data { get; init; }
    public List<InventoryTransaction>? TransactionList { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
}

public class GetCreditNoteSingleRequest : IRequest<GetCreditNoteSingleResult>
{
    public string? Id { get; init; }
}

public class GetCreditNoteSingleValidator : AbstractValidator<GetCreditNoteSingleRequest>
{
    public GetCreditNoteSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetCreditNoteSingleHandler : IRequestHandler<GetCreditNoteSingleRequest, GetCreditNoteSingleResult>
{
    private readonly IQueryContext _context;

    public GetCreditNoteSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetCreditNoteSingleResult> Handle(GetCreditNoteSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .CreditNote
            .AsNoTracking()
            .Include(x => x.SalesReturn)
                .ThenInclude(x => x.DeliveryOrder)
                    .ThenInclude(x => x.SalesOrder)
                        .ThenInclude(x => x.Customer)
            .Include(x => x.SalesReturn)
                .ThenInclude(x => x.DeliveryOrder)
                    .ThenInclude(x => x.SalesOrder)
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
            .Where(x => x.ModuleId == entity.SalesReturn.Id && x.ModuleName == nameof(SalesReturn))
            .AsQueryable();

        var transactionList = await queryTransactionList.ToListAsync(cancellationToken);

        var beforeTaxAmount = transactionList.Sum(x => x.Product?.UnitPrice * x.Movement);
        var taxAmount = entity?.SalesReturn?.DeliveryOrder?.SalesOrder?.Tax?.Percentage * beforeTaxAmount / 100;
        var afterTaxAmount = beforeTaxAmount + taxAmount;

        return new GetCreditNoteSingleResult
        {
            Data = entity,
            TransactionList = transactionList,
            BeforeTaxAmount = beforeTaxAmount,
            TaxAmount = taxAmount,
            AfterTaxAmount = afterTaxAmount
        };
    }
}