using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InvoiceManager.Queries;

public class GetInvoiceSingleProfile : Profile
{
    public GetInvoiceSingleProfile()
    {
    }
}

public class GetInvoiceSingleResult
{
    public Invoice? Data { get; init; }
}

public class GetInvoiceSingleRequest : IRequest<GetInvoiceSingleResult>
{
    public string? Id { get; init; }
}

public class GetInvoiceSingleValidator : AbstractValidator<GetInvoiceSingleRequest>
{
    public GetInvoiceSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetInvoiceSingleHandler : IRequestHandler<GetInvoiceSingleRequest, GetInvoiceSingleResult>
{
    private readonly IQueryContext _context;

    public GetInvoiceSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetInvoiceSingleResult> Handle(GetInvoiceSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Invoice
            .AsNoTracking()
            .Include(x => x.SalesOrder)
                .ThenInclude(x => x.Customer)
            .Include(x => x.SalesOrder)
                .ThenInclude(x => x.SalesOrderItemList)
                    .ThenInclude(x => x.Product)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetInvoiceSingleResult
        {
            Data = entity
        };
    }
}