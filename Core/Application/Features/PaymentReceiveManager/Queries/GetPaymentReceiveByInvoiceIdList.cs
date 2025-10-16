using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentReceiveManager.Queries;

public record GetPaymentReceiveByInvoiceIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public string? Description { get; init; }
    public DateTime? PaymentDate { get; init; }
    public string? PaymentMethodId { get; init; }
    public string? PaymentMethodName { get; init; }
    public double? PaymentAmount { get; init; }
    public PaymentReceiveStatus? Status { get; init; }
    public string? StatusName { get; init; }
    public string? InvoiceId { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentReceiveByInvoiceIdListProfile : Profile
{
    public GetPaymentReceiveByInvoiceIdListProfile()
    {
        CreateMap<PaymentReceive, GetPaymentReceiveByInvoiceIdListDto>()
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

public class GetPaymentReceiveByInvoiceIdListResult
{
    public List<GetPaymentReceiveByInvoiceIdListDto>? Data { get; init; }
}

public class GetPaymentReceiveByInvoiceIdListRequest : IRequest<GetPaymentReceiveByInvoiceIdListResult>
{
    public string? InvoiceId { get; init; }
}

public class GetPaymentReceiveByInvoiceIdListHandler : IRequestHandler<GetPaymentReceiveByInvoiceIdListRequest, GetPaymentReceiveByInvoiceIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentReceiveByInvoiceIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentReceiveByInvoiceIdListResult> Handle(GetPaymentReceiveByInvoiceIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentReceive
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.PaymentMethod)
            .Where(x => x.InvoiceId == request.InvoiceId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPaymentReceiveByInvoiceIdListDto>>(entities);

        return new GetPaymentReceiveByInvoiceIdListResult
        {
            Data = dtos
        };
    }
}