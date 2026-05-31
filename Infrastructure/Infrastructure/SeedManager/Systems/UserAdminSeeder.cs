using Application.Common.Services.SecurityManager;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Infrastructure.SeedManager.Systems;

public class UserAdminSeeder
{
    private readonly IdentitySettings _identitySettings;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminSeeder(
        IOptions<IdentitySettings> identitySettings,
        UserManager<ApplicationUser> userManager)
    {
        _identitySettings = identitySettings.Value;
        _userManager = userManager;
    }

    public async Task GenerateDataAsync()
    {
        var adminEmail = _identitySettings.DefaultAdmin.Email;
        var adminPassword = _identitySettings.DefaultAdmin.Password;

        if (await _userManager.FindByEmailAsync(adminEmail) != null)
        {
            return;
        }

        var applicationUser = new ApplicationUser(adminEmail, "Root", "Admin")
        {
            EmailConfirmed = true,
            PrimaryMenuPersona = TelecomMenuPersona.SysAdmin,
        };

        await _userManager.CreateAsync(applicationUser, adminPassword);
        await _userManager.AddToRoleAsync(applicationUser, TelecomRoles.Admin);
    }

    /// <summary>Ensures default admin has TelecomAdmin + SysAdmin persona (idempotent).</summary>
    public async Task AssignAllCatalogRolesToDefaultAdminAsync()
    {
        var adminEmail = _identitySettings.DefaultAdmin.Email;
        var applicationUser = await _userManager.FindByEmailAsync(adminEmail);
        if (applicationUser == null)
        {
            return;
        }

        if (!await _userManager.IsInRoleAsync(applicationUser, TelecomRoles.Admin))
        {
            await _userManager.AddToRoleAsync(applicationUser, TelecomRoles.Admin);
        }

        applicationUser.PrimaryMenuPersona = TelecomMenuPersona.SysAdmin;
        await _userManager.UpdateAsync(applicationUser);
    }
}
