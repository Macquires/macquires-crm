using Application.Common.CQS.Queries;
using Application.Common.Security;
using Application.Common.Extensions;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomSubscriptionTypeManager.Queries;

public record GetTelecomSubscriptionTypeListDto
{
    public string? Id { get; init; }
    public string? Code { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? DisplayColor { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTelecomSubscriptionTypeListProfile : Profile
{
    public GetTelecomSubscriptionTypeListProfile()
    {
        CreateMap<TelecomSubscriptionTypeLookup, GetTelecomSubscriptionTypeListDto>();
    }
}

public class GetTelecomSubscriptionTypeListResult
{
    public List<GetTelecomSubscriptionTypeListDto>? Data { get; init; }
}

public class GetTelecomSubscriptionTypeListRequest : IRequest<GetTelecomSubscriptionTypeListResult>, IRequireAnyPermission
{
    public bool IsDeleted { get; init; }

    /// <summary>When true, only <see cref="TelecomSubscriptionTypeLookup.IsActive"/> rows (for dropdowns).</summary>
    public bool ActiveOnly { get; init; }

    public IReadOnlyList<string> PermissionKeys => ReferenceDataPermissionSets.ReadAny;
}

public class GetTelecomSubscriptionTypeListHandler
    : IRequestHandler<GetTelecomSubscriptionTypeListRequest, GetTelecomSubscriptionTypeListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetTelecomSubscriptionTypeListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetTelecomSubscriptionTypeListResult> Handle(
        GetTelecomSubscriptionTypeListRequest request,
        CancellationToken cancellationToken)
    {
        var query = _context
            .TelecomSubscriptionTypeLookup
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var entities = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetTelecomSubscriptionTypeListDto>>(entities);

        return new GetTelecomSubscriptionTypeListResult { Data = dtos };
    }
}
