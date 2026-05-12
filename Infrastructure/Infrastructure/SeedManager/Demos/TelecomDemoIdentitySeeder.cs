using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.SeedManager.Demos;

/// <summary>One user per Syriatel telecom role for Thursday demos (idempotent).</summary>
public sealed class TelecomDemoIdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;

    public TelecomDemoIdentitySeeder(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task GenerateDataAsync()
    {
        const string password = "123456";

        var accounts = new (string Email, string First, string Last, string Role)[]
        {
            ("st-showroom@syriatelecom-demo.local", "معرض", "سيريتل", TelecomRoles.Showroom),
            ("st-backoffice@syriatelecom-demo.local", "مكتب", "خلفي", TelecomRoles.BackOffice),
            ("st-callcenter@syriatelecom-demo.local", "دعم", "خط", TelecomRoles.CallCenter),
            ("st-mis@syriatelecom-demo.local", "إدارة", "أرقام", TelecomRoles.Management),
        };

        foreach (var (email, first, last, role) in accounts)
        {
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                continue;
            }

            var user = new ApplicationUser(email, first, last)
            {
                EmailConfirmed = true,
            };

            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                continue;
            }

            var created = await _userManager.FindByEmailAsync(email);
            if (created == null)
            {
                continue;
            }

            if (!await _userManager.IsInRoleAsync(created, role))
            {
                await _userManager.AddToRoleAsync(created, role);
            }
        }
    }
}
