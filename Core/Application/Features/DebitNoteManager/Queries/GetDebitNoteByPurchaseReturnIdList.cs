using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DebitNoteManager.Queries;

public record GetDebitNoteByPurchaseReturnIdListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? DebitNoteDate { get; init; }
    public DebitNoteStatus? DebitNoteStatus { get; init; }
    public string? StatusName { get; init; }
    public string? Description { get; init; }
    public string? PurchaseReturnId { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetDebitNoteByPurchaseReturnIdListProfile : Profile
{
    public GetDebitNoteByPurchaseReturnIdListProfile()
    {
        CreateMap<DebitNote, GetDebitNoteByPurchaseReturnIdListDto>()
            .ForMember(
                dest => dest.StatusName,
                opt => opt.MapFrom(src => src.DebitNoteStatus.HasValue ? src.DebitNoteStatus.Value.ToFriendlyName() : string.Empty)
            );
    }
}

public class GetDebitNoteByPurchaseReturnIdListResult
{
    public List<GetDebitNoteByPurchaseReturnIdListDto>? Data { get; init; }
}

public class GetDebitNoteByPurchaseReturnIdListRequest : IRequest<GetDebitNoteByPurchaseReturnIdListResult>
{
    public string? PurchaseReturnId { get; init; }
}

public class GetDebitNoteByPurchaseReturnIdListHandler : IRequestHandler<GetDebitNoteByPurchaseReturnIdListRequest, GetDebitNoteByPurchaseReturnIdListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetDebitNoteByPurchaseReturnIdListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetDebitNoteByPurchaseReturnIdListResult> Handle(GetDebitNoteByPurchaseReturnIdListRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .DebitNote
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Where(x => x.PurchaseReturnId == request.PurchaseReturnId)
            .AsQueryable();

        var entities = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<GetDebitNoteByPurchaseReturnIdListDto>>(entities);

        return new GetDebitNoteByPurchaseReturnIdListResult
        {
            Data = dtos
        };
    }
}