using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;

public record GetDashboardWidgetListDto
{
    public string? Id { get; init; }
    public string? WidgetKey { get; init; }
    public string? TitleAr { get; init; }
    public string? TitleEn { get; init; }
    public string? Icon { get; init; }
    public string? ProviderKey { get; init; }
    public string? PersonasAllowed { get; init; }
    public DashboardWidgetGridSize GridSize { get; init; }
    public int SortOrder { get; init; }
    public DashboardWidgetKind WidgetKind { get; init; }
    public int? RefreshIntervalSeconds { get; init; }
    public string? CtaUrl { get; init; }
    public string? CtaLabelAr { get; init; }
    public string? CtaLabelEn { get; init; }
    public bool IsActive { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetDashboardWidgetListProfile : Profile
{
    public GetDashboardWidgetListProfile() =>
        CreateMap<DashboardWidget, GetDashboardWidgetListDto>();
}

public class GetDashboardWidgetListResult
{
    public List<GetDashboardWidgetListDto>? Data { get; init; }
}

public class GetDashboardWidgetListRequest : IRequest<GetDashboardWidgetListResult>, IRequireAnyPermission
{
    public bool IsDeleted { get; init; }
    public bool ActiveOnly { get; init; }
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.AdminAny;
}

public class GetDashboardWidgetListHandler
    : IRequestHandler<GetDashboardWidgetListRequest, GetDashboardWidgetListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetDashboardWidgetListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetDashboardWidgetListResult> Handle(
        GetDashboardWidgetListRequest request,
        CancellationToken cancellationToken)
    {
        var query = _context.DashboardWidget
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .AsQueryable();

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var entities = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.TitleAr)
            .ToListAsync(cancellationToken);

        return new GetDashboardWidgetListResult
        {
            Data = _mapper.Map<List<GetDashboardWidgetListDto>>(entities),
        };
    }
}
