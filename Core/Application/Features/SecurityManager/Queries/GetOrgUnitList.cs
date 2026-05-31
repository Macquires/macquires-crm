using Application.Common.CQS.Queries;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SecurityManager.Queries;

public class GetOrgUnitListResultDto
{
    public string? Id { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? ParentId { get; init; }
    public string? ParentNameAr { get; init; }
    public string? ManagerUserId { get; init; }
    public string? ManagerDisplayName { get; init; }
    public string? ManagerEmail { get; init; }
    public bool IsActive { get; init; }
    public int StaffCount { get; init; }
}

public class GetOrgUnitListResult
{
    public List<GetOrgUnitListResultDto>? Data { get; init; }
}

public class GetOrgUnitListRequest : IRequest<GetOrgUnitListResult>
{
}

public class GetOrgUnitListValidator : AbstractValidator<GetOrgUnitListRequest>
{
}

public class GetOrgUnitListHandler : IRequestHandler<GetOrgUnitListRequest, GetOrgUnitListResult>
{
    private readonly IQueryContext _query;
    private readonly ISecurityService _securityService;

    public GetOrgUnitListHandler(IQueryContext query, ISecurityService securityService)
    {
        _query = query;
        _securityService = securityService;
    }

    public async Task<GetOrgUnitListResult> Handle(GetOrgUnitListRequest request, CancellationToken cancellationToken)
    {
        var units = await _query.OrgUnit
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.NameAr)
            .ToListAsync(cancellationToken);

        var parentNames = units.ToDictionary(x => x.Id, x => x.NameAr);
        var users = await _securityService.GetUserListAsync(null, cancellationToken);
        var userById = users
            .Where(u => !string.IsNullOrEmpty(u.Id))
            .ToDictionary(u => u.Id!, u => u);

        var staffByUnit = users
            .Where(u => !string.IsNullOrEmpty(u.OrgUnitId))
            .GroupBy(u => u.OrgUnitId!)
            .ToDictionary(g => g.Key, g => g.Count());

        var data = units.Select(x =>
        {
            userById.TryGetValue(x.ManagerUserId ?? "", out var mgr);
            parentNames.TryGetValue(x.ParentId ?? "", out var parentName);

            return new GetOrgUnitListResultDto
            {
                Id = x.Id,
                NameAr = x.NameAr,
                NameEn = x.NameEn,
                ParentId = x.ParentId,
                ParentNameAr = string.IsNullOrEmpty(x.ParentId) ? null : parentName,
                ManagerUserId = x.ManagerUserId,
                ManagerDisplayName = mgr == null
                    ? null
                    : $"{mgr.FirstName} {mgr.LastName}".Trim(),
                ManagerEmail = mgr?.Email,
                IsActive = x.IsActive,
                StaffCount = staffByUnit.TryGetValue(x.Id, out var count) ? count : 0,
            };
        }).ToList();

        return new GetOrgUnitListResult { Data = data };
    }
}
