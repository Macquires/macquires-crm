using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CreditNoteManager.Queries;

public record GetCreditNoteListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? CreditNoteDate { get; init; }
    public CreditNoteStatus? CreditNoteStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? SalesReturnId { get; init; }
    public string? SalesReturnNumber { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetCreditNoteListProfile : Profile
{
    public GetCreditNoteListProfile()
    {
        CreateMap<CreditNote, GetCreditNoteListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.CreditNoteStatus.HasValue ? src.CreditNoteStatus.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.SalesReturnNumber,
                opt => opt.MapFrom(src => src.SalesReturn != null ? src.SalesReturn.Number : string.Empty)
            );
    }
}

public class GetCreditNoteListResult
{
    public List<GetCreditNoteListDto>? Data { get; init; }
}

public class GetCreditNoteListRequest : IRequest<GetCreditNoteListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetCreditNoteListHandler : IRequestHandler<GetCreditNoteListRequest, GetCreditNoteListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetCreditNoteListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetCreditNoteListResult> Handle(GetCreditNoteListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .CreditNote
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.SalesReturn)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetCreditNoteListDto>>(entities);

        return new GetCreditNoteListResult
        {
            Data = dtos
        };
    }
}