// Demo-only — profile users for Syria Telecom demo tenants.
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.SeedManager.Demos;

public class UserSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserSeeder(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task GenerateDataAsync()
    {
        var userNames = new List<string>
        {
            "نورا", "ليث", "ميس", "طارق", "رانيا",
            "بشار", "هبة", "كرم", "سلمى", "يامن",
            "غادة", "فادي", "لينا", "زيد", "منار",
            "عمر", "دانة", "سامي", "ريم", "مازن"
        };

        const string defaultPassword = "123456";

        for (var i = 0; i < userNames.Count; i++)
        {
            var displayName = userNames[i];
            var email = $"st-user{(i + 1):D2}@syriatelecom-demo.local";

            if (await _userManager.FindByEmailAsync(email) == null)
            {
                var applicationUser = new ApplicationUser(email, displayName, "User")
                {
                    EmailConfirmed = true
                };

                await _userManager.CreateAsync(applicationUser, defaultPassword);

                var role = RoleHelper.GetProfileRole();
                if (!await _userManager.IsInRoleAsync(applicationUser, role))
                {
                    await _userManager.AddToRoleAsync(applicationUser, role);
                }
            }
        }
    }
}
