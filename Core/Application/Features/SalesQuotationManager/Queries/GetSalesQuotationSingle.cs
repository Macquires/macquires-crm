using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesQuotationManager.Queries;


public class GetSalesQuotationSingleProfile : Profile
{
    public GetSalesQuotationSingleProfile()
    {
    }
}

public class GetSalesQuotationSingleResult
{
    public SalesQuotation? Data { get; init; }
}

public class GetSalesQuotationSingleRequest : IRequest<GetSalesQuotationSingleResult>
{
    public string? Id { get; init; }
}

public class GetSalesQuotationSingleValidator : AbstractValidator<GetSalesQuotationSingleRequest>
{
    public GetSalesQuotationSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetSalesQuotationSingleHandler : IRequestHandler<GetSalesQuotationSingleRequest, GetSalesQuotationSingleResult>
{
    private readonly IQueryContext _context;

    public GetSalesQuotationSingleHandler(
        IQueryContext context
        )
    {
        _context = context;
    }

    public async Task<GetSalesQuotationSingleResult> Handle(GetSalesQuotationSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .SalesQuotation
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Tax)
            .Include(x => x.SalesQuotationItemList.Where(item => !item.IsDeleted))
                .ThenInclude(x => x.Product)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetSalesQuotationSingleResult
        {
            Data = entity
        };
    }
}