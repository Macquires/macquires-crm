using Application.Common.Security;

using Application.Common.Services.SecurityManager;

using Application.Common.Telecom;

using Domain.Entities;

using Infrastructure.DataAccessManager.EFCore.Contexts;

using Infrastructure.SecurityManager.AspNetIdentity;

using Infrastructure.SecurityManager.Roles;

using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;



namespace Infrastructure.SeedManager.Systems;



/// <summary>

/// Purges legacy template roles/permissions and seeds the five-role telecom RBAC matrix (idempotent).

/// </summary>

public class RolePermissionSeeder

{

    private readonly DataContext _context;

    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly UserManager<ApplicationUser> _userManager;



    public RolePermissionSeeder(

        DataContext context,

        RoleManager<IdentityRole> roleManager,

        UserManager<ApplicationUser> userManager)

    {

        _context = context;

        _roleManager = roleManager;

        _userManager = userManager;

    }



    public async Task GenerateDataAsync()

    {

        await PurgeLegacyRolesAndPermissionsAsync();

        await EnsureStandardRolesAsync();

        await ReseedRolePermissionsAsync();

        await SyncUserPersonasFromRolesAsync();

    }



    private async Task PurgeLegacyRolesAndPermissionsAsync()

    {

        var allowedRoles = new HashSet<string>(TelecomEnterpriseRoleMatrix.StandardRoles, StringComparer.OrdinalIgnoreCase);

        var validKeys = PermissionCatalog.All.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);



        var orphanRolePerms = await _context.RolePermission

            .Where(rp => !allowedRoles.Contains(rp.RoleName) || !validKeys.Contains(rp.PermissionKey))

            .ToListAsync();

        if (orphanRolePerms.Count > 0)

        {

            _context.RolePermission.RemoveRange(orphanRolePerms);

            await _context.SaveChangesAsync();

        }



        var legacyRoles = await _context.Roles

            .Where(r => r.Name != null && !allowedRoles.Contains(r.Name))

            .ToListAsync();



        if (legacyRoles.Count == 0)

        {

            return;

        }



        var legacyRoleIds = legacyRoles.Select(r => r.Id).ToList();

        var legacyUserRoles = await _context.UserRoles

            .Where(ur => legacyRoleIds.Contains(ur.RoleId))

            .ToListAsync();

        if (legacyUserRoles.Count > 0)

        {

            _context.UserRoles.RemoveRange(legacyUserRoles);

            await _context.SaveChangesAsync();

        }



        foreach (var role in legacyRoles)

        {

            await _roleManager.DeleteAsync(role);

        }

    }



    private async Task EnsureStandardRolesAsync()

    {

        foreach (var roleName in TelecomEnterpriseRoleMatrix.StandardRoles)

        {

            if (!await _roleManager.RoleExistsAsync(roleName))

            {

                await _roleManager.CreateAsync(new IdentityRole(roleName));

            }

        }

    }



    private async Task ReseedRolePermissionsAsync()

    {

        var existing = await _context.RolePermission.ToListAsync();

        if (existing.Count > 0)

        {

            _context.RolePermission.RemoveRange(existing);

            await _context.SaveChangesAsync();

        }



        var now = DateTime.UtcNow;

        foreach (var (roleName, keys) in PermissionCatalog.DefaultRoleGrants)

        {

            foreach (var key in keys)

            {

                await _context.RolePermission.AddAsync(new RolePermission

                {

                    RoleName = roleName,

                    PermissionKey = key,

                    GrantedAtUtc = now,

                });

            }

        }



        await _context.SaveChangesAsync();

    }



    private async Task SyncUserPersonasFromRolesAsync()

    {

        var users = await _userManager.Users.ToListAsync();

        foreach (var user in users)

        {

            var roles = await _userManager.GetRolesAsync(user);

            var persona = TelecomPersonaResolver.ResolvePrimary(roles.ToList(), null);

            if (!persona.HasValue)

            {

                continue;

            }



            if (user.PrimaryMenuPersona != persona)

            {

                user.PrimaryMenuPersona = persona;

                await _userManager.UpdateAsync(user);

            }

        }

    }

}


