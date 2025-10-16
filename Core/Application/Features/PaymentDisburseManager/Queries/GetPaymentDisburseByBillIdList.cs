using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentDisburseManager.Queries;

public record GetPaymentDisburseByBillIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public string? PaymentMethodName { get; init; }
    public double? PaymentAmount { get; init; }
    public PaymentDisburseStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? BillId { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentDisburseByBillIdListProfile : Profile
{
    public GetPaymentDisburseByBillIdListProfile()
    {
        CreateMap<PaymentDisburse, GetPaymentDisburseByBillIdListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PaymentMethodName,
                opt => opt.MapFrom(src => src.PaymentMethod != null ? src.PaymentMethod.Name : string.Empty)
            );
    }
}

public class GetPaymentDisburseByBillIdListResult
{
    public List<GetPaymentDisburseByBillIdListDto>? Data { get; init; }
}

public class GetPaymentDisburseByBillIdListRequest : IRequest<GetPaymentDisburseByBillIdListResult>
{
    public string? BillId { get; init; }
}

public class GetPaymentDisburseByBillIdListHandler : IRequestHandler<GetPaymentDisburseByBillIdListRequest, GetPaymentDisburseByBillIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentDisburseByBillIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentDisburseByBillIdListResult> Handle(GetPaymentDisburseByBillIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentDisburse
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.PaymentMethod)
            .Where(x => x.BillId == request.BillId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPaymentDisburseByBillIdListDto>>(entities);

        return new GetPaymentDisburseByBillIdListResult
        {
            Data = dtos
        };
    }
}