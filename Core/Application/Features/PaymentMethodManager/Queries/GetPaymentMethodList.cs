using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentMethodManager.Queries;

public record GetPaymentMethodListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentMethodListProfile : Profile
{
    public GetPaymentMethodListProfile()
    {
        CreateMap<PaymentMethod, GetPaymentMethodListDto>();
    }
}

public class GetPaymentMethodListResult
{
    public List<GetPaymentMethodListDto>? Data { get; init; }
}

public class GetPaymentMethodListRequest : IRequest<GetPaymentMethodListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetPaymentMethodListHandler : IRequestHandler<GetPaymentMethodListRequest, GetPaymentMethodListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentMethodListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentMethodListResult> Handle(GetPaymentMethodListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentMethod
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPaymentMethodListDto>>(entities);

        return new GetPaymentMethodListResult
        {
            Data = dtos
        };
    }


}



