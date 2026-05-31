using Application.Common.Security;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Security;

public class PermissionEvaluator : IPermissionEvaluator
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly DataContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMemoryCache _cache;

    public PermissionEvaluator(DataContext context, UserManager<ApplicationUser> userManager, IMemoryCache cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionKey, CancellationToken cancellationToken = default)
    {
        var keys = await GetUserPermissionKeysAsync(userId, cancellationToken);
        return keys.Contains(permissionKey, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionKeysAsync(string userId, CancellationToken cancellationToken = default)
    {
        var set = await GetUserPermissionSetAsync(userId, cancellationToken);
        return set.ToList();
    }

    public async Task<IReadOnlyList<string>> GetUserRoleNamesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return [];
        }

        return (await _userManager.GetRolesAsync(user)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            $"RolePermissions:{roleName}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await _context.RolePermission
                    .AsNoTracking()
                    .Where(rp => rp.RoleName == roleName)
                    .Select(rp => rp.PermissionKey)
                    .ToListAsync(cancellationToken);
            }) ?? [];
    }

    private async Task<HashSet<string>> GetUserPermissionSetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return [];
        }

        var roles = await _userManager.GetRolesAsync(user);
        var roleKey = string.Join('|', roles.OrderBy(r => r));

        return await _cache.GetOrCreateAsync(
            $"UserPermissions:{userId}:{roleKey}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                if (roles.Count == 0)
                {
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                var keys = await _context.RolePermission
                    .AsNoTracking()
                    .Where(rp => roles.Contains(rp.RoleName))
                    .Select(rp => rp.PermissionKey)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }) ?? [];
    }

    public async Task UpdateRolePermissionsAsync(
        string roleName,
        IReadOnlyList<string> permissionKeys,
        string? grantedById,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.RolePermission
            .Where(rp => rp.RoleName == roleName)
            .ToListAsync(cancellationToken);

        _context.RolePermission.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var key in permissionKeys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _context.RolePermission.AddAsync(new Domain.Entities.RolePermission
            {
                RoleName = roleName,
                PermissionKey = key,
                GrantedAtUtc = now,
                GrantedById = grantedById,
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove($"RolePermissions:{roleName}");
    }
}
