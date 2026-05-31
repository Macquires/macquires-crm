using Application.Common.Security;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.SeedManager.Systems;

public class RoleSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleSeeder(RoleManager<IdentityRole> roleManager) => _roleManager = roleManager;

    public async Task GenerateDataAsync()
    {
        foreach (var role in TelecomEnterpriseRoleMatrix.StandardRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
