using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using AutoMapper;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.VasManager.Queries;

public record GetValueAddedServiceListDto
{
    public string? Id { get; init; }
    public string? ServiceCode { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public decimal MonthlyFee { get; init; }
    public bool IsActive { get; init; }
    public string? HlrCommandTemplate { get; init; }
    public int SortOrder { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetValueAddedServiceListProfile : Profile
{
    public GetValueAddedServiceListProfile() =>
        CreateMap<TelecomValueAddedService, GetValueAddedServiceListDto>();
}

public class GetValueAddedServiceListResult
{
    public List<GetValueAddedServiceListDto>? Data { get; init; }
}

public class GetValueAddedServiceListRequest : IRequest<GetValueAddedServiceListResult>, IRequireAnyPermission
{
    public bool IsDeleted { get; init; }
    public bool ActiveOnly { get; init; }

    public IReadOnlyList<string> PermissionKeys => ActiveOnly
        ?
        [
            PermissionCatalog.TelecomVasToggle,
            PermissionCatalog.TelecomVasManage,
            PermissionCatalog.TelecomCustomerProvisioning,
            PermissionCatalog.CustomerView,
        ]
        : [PermissionCatalog.TelecomVasManage];
}

public class GetValueAddedServiceListHandler : IRequestHandler<GetValueAddedServiceListRequest, GetValueAddedServiceListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetValueAddedServiceListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetValueAddedServiceListResult> Handle(
        GetValueAddedServiceListRequest request,
        CancellationToken cancellationToken)
    {
        var query = _context.TelecomValueAddedService
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var entities = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ServiceCode)
            .ToListAsync(cancellationToken);

        return new GetValueAddedServiceListResult { Data = _mapper.Map<List<GetValueAddedServiceListDto>>(entities) };
    }
}
