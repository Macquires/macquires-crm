using Application.Common.Telecom.Analytics;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telecom.Analytics;

public sealed class WorkforceUserReadService : IWorkforceUserReadService
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(30);

    private readonly DataContext _context;

    public WorkforceUserReadService(DataContext context) => _context = context;

    public async Task<IReadOnlyList<WorkforceUserSnapshot>> GetUsersInBranchesAsync(
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken = default)
    {
        if (branchIds.Count == 0)
        {
            return [];
        }

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsDeleted != true && u.OrgUnitId != null && branchIds.Contains(u.OrgUnitId))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return [];
        }

        var userIds = users.Select(u => u.Id).ToList();
        var orgUnitIds = users.Select(u => u.OrgUnitId!).Distinct().ToList();
        var orgUnits = await _context.OrgUnit.AsNoTracking()
            .Where(o => orgUnitIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.NameAr, cancellationToken);

        var roleRows = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync(cancellationToken);

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? "").Where(n => n.Length > 0).ToList());

        var onlineCutoff = DateTime.UtcNow.Subtract(OnlineThreshold);

        return users.Select(u =>
        {
            var lastSeen = u.LastActivityAtUtc ?? u.LastLoginAtUtc;
            rolesByUser.TryGetValue(u.Id, out var roles);
            orgUnits.TryGetValue(u.OrgUnitId ?? "", out var ouName);

            return new WorkforceUserSnapshot
            {
                UserId = u.Id,
                DisplayName = $"{u.FirstName} {u.LastName}".Trim(),
                OrgUnitId = u.OrgUnitId,
                OrgUnitNameAr = ouName,
                Roles = roles ?? [],
                LastActivityUtc = lastSeen,
                IsOnline = u.IsBlocked != true && lastSeen.HasValue && lastSeen.Value >= onlineCutoff,
            };
        }).ToList();
    }

    public async Task<int> CountOnlineInBranchesAsync(
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken = default)
    {
        if (branchIds.Count == 0)
        {
            return 0;
        }

        var onlineCutoff = DateTime.UtcNow.Subtract(OnlineThreshold);
        return await _context.Users
            .AsNoTracking()
            .CountAsync(
                u => u.IsDeleted != true
                    && u.IsBlocked != true
                    && u.OrgUnitId != null
                    && branchIds.Contains(u.OrgUnitId)
                    && ((u.LastActivityAtUtc ?? u.LastLoginAtUtc) >= onlineCutoff),
                cancellationToken);
    }
}
