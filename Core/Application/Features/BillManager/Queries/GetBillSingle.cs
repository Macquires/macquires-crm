using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BillManager.Queries;

public class GetBillSingleProfile : Profile
{
    public GetBillSingleProfile()
    {
    }
}

public class GetBillSingleResult
{
    public Bill? Data { get; init; }
}

public class GetBillSingleRequest : IRequest<GetBillSingleResult>
{
    public string? Id { get; init; }
}

public class GetBillSingleValidator : AbstractValidator<GetBillSingleRequest>
{
    public GetBillSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetBillSingleHandler : IRequestHandler<GetBillSingleRequest, GetBillSingleResult>
{
    private readonly IQueryContext _context;

    public GetBillSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetBillSingleResult> Handle(GetBillSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Bill
            .AsNoTracking()
            .Include(x => x.PurchaseOrder)
                .ThenInclude(x => x.Vendor)
            .Include(x => x.PurchaseOrder)
                .ThenInclude(x => x.PurchaseOrderItemList)
                    .ThenInclude(x => x.Product)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetBillSingleResult
        {
            Data = entity
        };
    }
}