using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseRequisitionManager.Queries;


public class GetPurchaseRequisitionSingleProfile : Profile
{
    public GetPurchaseRequisitionSingleProfile()
    {
    }
}

public class GetPurchaseRequisitionSingleResult
{
    public PurchaseRequisition? Data { get; init; }
}

public class GetPurchaseRequisitionSingleRequest : IRequest<GetPurchaseRequisitionSingleResult>
{
    public string? Id { get; init; }
}

public class GetPurchaseRequisitionSingleValidator : AbstractValidator<GetPurchaseRequisitionSingleRequest>
{
    public GetPurchaseRequisitionSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetPurchaseRequisitionSingleHandler : IRequestHandler<GetPurchaseRequisitionSingleRequest, GetPurchaseRequisitionSingleResult>
{
    private readonly IQueryContext _context;

    public GetPurchaseRequisitionSingleHandler(
        IQueryContext context
        )
    {
        _context = context;
    }

    public async Task<GetPurchaseRequisitionSingleResult> Handle(GetPurchaseRequisitionSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PurchaseRequisition
            .AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.Tax)
            .Include(x => x.PurchaseRequisitionItemList.Where(item => !item.IsDeleted))
                .ThenInclude(x => x.Product)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetPurchaseRequisitionSingleResult
        {
            Data = entity
        };
    }
}