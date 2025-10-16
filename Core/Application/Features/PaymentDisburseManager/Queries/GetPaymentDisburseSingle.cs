using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentDisburseManager.Queries;

public class GetPaymentDisburseSingleProfile : Profile
{
    public GetPaymentDisburseSingleProfile()
    {
    }
}

public class GetPaymentDisburseSingleResult
{
    public PaymentDisburse? Data { get; init; }
}

public class GetPaymentDisburseSingleRequest : IRequest<GetPaymentDisburseSingleResult>
{
    public string? Id { get; init; }
}

public class GetPaymentDisburseSingleValidator : AbstractValidator<GetPaymentDisburseSingleRequest>
{
    public GetPaymentDisburseSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetPaymentDisburseSingleHandler : IRequestHandler<GetPaymentDisburseSingleRequest, GetPaymentDisburseSingleResult>
{
    private readonly IQueryContext _context;

    public GetPaymentDisburseSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetPaymentDisburseSingleResult> Handle(GetPaymentDisburseSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentDisburse
            .AsNoTracking()
            .Include(x => x.Bill)
                .ThenInclude(x => x.PurchaseOrder)
                    .ThenInclude(x => x.PurchaseOrderItemList)
                        .ThenInclude(x => x.Product)
            .Include(x => x.Bill)
                .ThenInclude(x => x.PurchaseOrder)
                    .ThenInclude(x => x.Vendor)
            .Include(x => x.PaymentMethod)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetPaymentDisburseSingleResult
        {
            Data = entity
        };
    }
}