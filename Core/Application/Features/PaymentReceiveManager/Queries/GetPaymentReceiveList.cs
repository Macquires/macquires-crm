using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentReceiveManager.Queries;

public record GetPaymentReceiveListDto
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
    public string? InvoiceNumber { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPaymentReceiveListProfile : Profile
{
    public GetPaymentReceiveListProfile()
    {
        CreateMap<PaymentReceive, GetPaymentReceiveListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.Status.HasValue ? src.Status.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PaymentMethodName,
                opt => opt.MapFrom(src => src.PaymentMethod != null ? src.PaymentMethod.Name : string.Empty)
            )
            .ForMember(
                dest => dest.InvoiceNumber,
                opt => opt.MapFrom(src => src.Invoice != null ? src.Invoice.Number : string.Empty)
            );
    }
}

public class GetPaymentReceiveListResult
{
    public List<GetPaymentReceiveListDto>? Data { get; init; }
}

public class GetPaymentReceiveListRequest : IRequest<GetPaymentReceiveListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetPaymentReceiveListHandler : IRequestHandler<GetPaymentReceiveListRequest, GetPaymentReceiveListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetPaymentReceiveListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetPaymentReceiveListResult> Handle(GetPaymentReceiveListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentReceive
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PaymentMethod)
            .Include(x => x.Invoice)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetPaymentReceiveListDto>>(entities);

        return new GetPaymentReceiveListResult
        {
            Data = dtos
        };
    }
}