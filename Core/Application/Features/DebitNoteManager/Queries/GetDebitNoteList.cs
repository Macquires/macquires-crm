using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DebitNoteManager.Queries;

public record GetDebitNoteListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? DebitNoteDate { get; init; }
    public DebitNoteStatus? DebitNoteStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? PurchaseReturnId { get; init; }
    public string? PurchaseReturnNumber { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetDebitNoteListProfile : Profile
{
    public GetDebitNoteListProfile()
    {
        CreateMap<DebitNote, GetDebitNoteListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.DebitNoteStatus.HasValue ? src.DebitNoteStatus.Value.ToFriendlyName() : string.Empty)
            )
            .ForMember(
                dest => dest.PurchaseReturnNumber,
                opt => opt.MapFrom(src => src.PurchaseReturn != null ? src.PurchaseReturn.Number : string.Empty)
            );
    }
}

public class GetDebitNoteListResult
{
    public List<GetDebitNoteListDto>? Data { get; init; }
}

public class GetDebitNoteListRequest : IRequest<GetDebitNoteListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetDebitNoteListHandler : IRequestHandler<GetDebitNoteListRequest, GetDebitNoteListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetDebitNoteListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetDebitNoteListResult> Handle(GetDebitNoteListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .DebitNote
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.PurchaseReturn)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetDebitNoteListDto>>(entities);

        return new GetDebitNoteListResult
        {
            Data = dtos
        };
    }
}