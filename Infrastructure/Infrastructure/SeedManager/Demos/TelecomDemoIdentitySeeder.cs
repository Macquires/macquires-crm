using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Syriatel telecom demo accounts (password <c>123456</c> for all).
/// Includes strategic MIS personas for <c>/Telecom/StrategicAnalytics</c>.
/// </summary>
public sealed class TelecomDemoIdentitySeeder
{
    public const string DemoPassword = "123456";

    private readonly UserManager<ApplicationUser> _userManager;

    public TelecomDemoIdentitySeeder(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task GenerateDataAsync()
    {
        var accounts = new (string Email, string First, string Last, string Role, TelecomMenuPersona? Persona)[]
        {
            ("st-showroom@syriatelecom-demo.local", "معرض", "سيريتل", TelecomRoles.Showroom, null),
            ("st-backoffice@syriatelecom-demo.local", "مكتب", "خلفي", TelecomRoles.BackOffice, null),
            ("st-callcenter@syriatelecom-demo.local", "دعم", "خط", TelecomRoles.CallCenter, null),
            ("st-mis@syriatelecom-demo.local", "أحمد", "الشوا", TelecomRoles.Management, TelecomMenuPersona.Executive),
            ("st-regional-north@syriatelecom-demo.local", "مدير", "الشمال", TelecomRoles.Management, TelecomMenuPersona.Executive),
            ("st-regional-central@syriatelecom-demo.local", "مدير", "الوسطى", TelecomRoles.Management, TelecomMenuPersona.Executive),
            ("st-regional-coast@syriatelecom-demo.local", "مدير", "الساحل", TelecomRoles.Management, TelecomMenuPersona.Executive),
            ("st-branch-mezzeh@syriatelecom-demo.local", "مدير فرع", "المزة", TelecomRoles.Management, TelecomMenuPersona.Executive),
            ("st-branch-aleppo@syriatelecom-demo.local", "مدير فرع", "حلب", TelecomRoles.Management, TelecomMenuPersona.Executive),
            ("st-branch-tartus@syriatelecom-demo.local", "مدير فرع", "طرطوس", TelecomRoles.Management, TelecomMenuPersona.Executive),
        };

        foreach (var (email, first, last, role, personaOverride) in accounts)
        {
            await EnsureUserAsync(email, first, last, role, personaOverride);
        }
    }

    private async Task EnsureUserAsync(
        string email,
        string first,
        string last,
        string role,
        TelecomMenuPersona? personaOverride)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser(email, first, last) { EmailConfirmed = true };
            var create = await _userManager.CreateAsync(user, DemoPassword);
            if (!create.Succeeded)
            {
                return;
            }

            user = await _userManager.FindByEmailAsync(email);
        }

        if (user == null)
        {
            return;
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        var persona = personaOverride ?? TelecomPersonaRoleMapper.GetPersonaForRole(role);
        if (persona.HasValue && user.PrimaryMenuPersona != persona)
        {
            user.PrimaryMenuPersona = persona;
            await _userManager.UpdateAsync(user);
        }
    }
}
