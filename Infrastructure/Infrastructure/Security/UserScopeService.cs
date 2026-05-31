using Application.Common.Security;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public class UserScopeService : IUserScopeService
{
    private readonly DataContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserScopeService(DataContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> IsUnrestrictedAdminAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.Contains(TelecomRoles.Admin, StringComparer.OrdinalIgnoreCase)
            || roles.Contains(TelecomRoles.Management, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HashSet<string>> GetVisibleUserIdsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (await IsUnrestrictedAdminAsync(userId, cancellationToken))
        {
            var all = await _context.Users
                .Where(u => u.IsDeleted != true)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            return all.ToHashSet(StringComparer.Ordinal);
        }

        var visible = new HashSet<string>(StringComparer.Ordinal) { userId };
        await CollectSubordinatesAsync(userId, visible, cancellationToken);
        return visible;
    }

    public async Task<bool> CanAccessUserAsync(string actorUserId, string targetUserId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(actorUserId, targetUserId, StringComparison.Ordinal))
        {
            return true;
        }

        var visible = await GetVisibleUserIdsAsync(actorUserId, cancellationToken);
        return visible.Contains(targetUserId);
    }

    private async Task CollectSubordinatesAsync(string managerId, HashSet<string> acc, CancellationToken cancellationToken)
    {
        var direct = await _context.Users
            .Where(u => u.ManagerUserId == managerId && u.IsDeleted != true)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var id in direct)
        {
            if (acc.Add(id))
            {
                await CollectSubordinatesAsync(id, acc, cancellationToken);
            }
        }
    }
}
