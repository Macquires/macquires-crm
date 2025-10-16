using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentReceiveManager.Queries;

public class GetPaymentReceiveSingleProfile : Profile
{
    public GetPaymentReceiveSingleProfile()
    {
    }
}

public class GetPaymentReceiveSingleResult
{
    public PaymentReceive? Data { get; init; }
}

public class GetPaymentReceiveSingleRequest : IRequest<GetPaymentReceiveSingleResult>
{
    public string? Id { get; init; }
}

public class GetPaymentReceiveSingleValidator : AbstractValidator<GetPaymentReceiveSingleRequest>
{
    public GetPaymentReceiveSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetPaymentReceiveSingleHandler : IRequestHandler<GetPaymentReceiveSingleRequest, GetPaymentReceiveSingleResult>
{
    private readonly IQueryContext _context;

    public GetPaymentReceiveSingleHandler(
        IQueryContext context
    )
    {
        _context = context;
    }

    public async Task<GetPaymentReceiveSingleResult> Handle(GetPaymentReceiveSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentReceive
            .AsNoTracking()
            .Include(x => x.Invoice)
                .ThenInclude(x => x.SalesOrder)
                    .ThenInclude(x => x.SalesOrderItemList)
                        .ThenInclude(x => x.Product)
            .Include(x => x.Invoice)
                .ThenInclude(x => x.SalesOrder)
                    .ThenInclude(x => x.Customer)
            .Include(x => x.PaymentMethod)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetPaymentReceiveSingleResult
        {
            Data = entity
        };
    }
}