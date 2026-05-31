using Application.Common.Security;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public class StrategicDataScopeService : IStrategicDataScopeService
{
    private readonly DataContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StrategicDataScopeService(DataContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<StrategicDataScope> ResolveScopeAsync(
        string userId,
        string? requestedRegionId,
        string? requestedBranchId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("المستخدم غير موجود.");

        var roles = await _userManager.GetRolesAsync(user);
        var orgUnits = await _context.OrgUnit.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var managedHqId = orgUnits
            .FirstOrDefault(x => x.Kind == OrgUnitKind.Headquarters && x.ManagerUserId == userId)?.Id;

        var managedRegionId = orgUnits
            .FirstOrDefault(x => x.Kind == OrgUnitKind.Region && x.ManagerUserId == userId)?.Id;

        OrgUnitKind? userOrgKind = null;
        if (!string.IsNullOrEmpty(user.OrgUnitId))
        {
            userOrgKind = orgUnits.FirstOrDefault(x => x.Id == user.OrgUnitId)?.Kind;
        }

        var accessLevel = StrategicDataScopeResolver.DeriveAccessLevel(
            roles.ToList(),
            user.OrgUnitId,
            managedRegionId,
            managedHqId,
            userOrgKind);

        if (accessLevel == StrategicAccessLevel.RegionalDirector
            && !string.IsNullOrWhiteSpace(requestedRegionId)
            && !string.Equals(requestedRegionId.Trim(), managedRegionId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("لا يمكنك الاطلاع على بيانات خارج إقليمك.");
        }

        return StrategicDataScopeResolver.Resolve(
            accessLevel,
            orgUnits,
            managedRegionId,
            user.OrgUnitId,
            requestedRegionId,
            requestedBranchId);
    }
}
