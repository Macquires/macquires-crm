using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentDisburseManager.Queries;

public record GetPaymentDisburseListDto
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
    public string? BillNumber { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentDisburseListProfile : Profile
{
    public GetPaymentDisburseListProfile()
    {
        CreateMap<PaymentDisburse, GetPaymentDisburseListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PaymentMethodName,
                opt => opt.MapFrom(src => src.PaymentMethod != null ? src.PaymentMethod.Name : string.Empty)
            )
            .ForMember(
                dest => dest.BillNumber,
                opt => opt.MapFrom(src => src.Bill != null ? src.Bill.Number : string.Empty)
            );
    }
}

public class GetPaymentDisburseListResult
{
    public List<GetPaymentDisburseListDto>? Data { get; init; }
}

public class GetPaymentDisburseListRequest : IRequest<GetPaymentDisburseListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetPaymentDisburseListHandler : IRequestHandler<GetPaymentDisburseListRequest, GetPaymentDisburseListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentDisburseListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentDisburseListResult> Handle(GetPaymentDisburseListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentDisburse
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PaymentMethod)
            .Include(x => x.Bill)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPaymentDisburseListDto>>(entities);

        return new GetPaymentDisburseListResult
        {
            Data = dtos
        };
    }
}