using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CreditNoteManager.Queries;

public record GetCreditNoteBySalesReturnIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? CreditNoteDate { get; init; }
    public CreditNoteStatus? CreditNoteStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? SalesReturnId { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetCreditNoteBySalesReturnIdListProfile : Profile
{
    public GetCreditNoteBySalesReturnIdListProfile()
    {
        CreateMap<CreditNote, GetCreditNoteBySalesReturnIdListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.CreditNoteStatus.HasValue ? src.CreditNoteStatus.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetCreditNoteBySalesReturnIdListResult
{
    public List<GetCreditNoteBySalesReturnIdListDto>? Data { get; init; }
}

public class GetCreditNoteBySalesReturnIdListRequest : IRequest<GetCreditNoteBySalesReturnIdListResult>
{
    public string? SalesReturnId { get; init; }
}

public class GetCreditNoteBySalesReturnIdListHandler : IRequestHandler<GetCreditNoteBySalesReturnIdListRequest, GetCreditNoteBySalesReturnIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetCreditNoteBySalesReturnIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetCreditNoteBySalesReturnIdListResult> Handle(GetCreditNoteBySalesReturnIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .CreditNote
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Where(x => x.SalesReturnId == request.SalesReturnId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetCreditNoteBySalesReturnIdListDto>>(entities);

        return new GetCreditNoteBySalesReturnIdListResult
        {
            Data = dtos
        };
    }
}