using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.AspNetIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Systems;

public class OrgUnitSeeder
{
    private readonly DataContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrgUnitSeeder(DataContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task GenerateDataAsync()
    {
        await EnsureStrategicOrgTreeAsync();
        await LinkDemoUsersAsync();
        await LinkCustomersToBranchesAsync();
    }

    private async Task EnsureStrategicOrgTreeAsync()
    {
        var hq = await _context.OrgUnit
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.ParentId == null);

        if (hq == null)
        {
            hq = new OrgUnit
            {
                NameAr = "المقر الرئيسي — سيريتل",
                NameEn = "Syriatel HQ",
                Kind = OrgUnitKind.Headquarters,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            await _context.OrgUnit.AddAsync(hq);
            await _context.SaveChangesAsync();
        }
        else
        {
            hq.Kind = OrgUnitKind.Headquarters;
        }

        var regionDefs = new (string NameAr, string NameEn, (string Ar, string En)[] Branches)[]
        {
            ("المنطقة الوسطى", "Central Region",
            [
                ("فرع دمشق — المزة", "Damascus — Mezzeh"),
                ("فرع دمشق — أبو رمانة", "Damascus — Abu Rummaneh"),
                ("فرع دمشق — الحجاز", "Damascus — Hijaz"),
            ]),
            ("المنطقة الشمالية", "Northern Region",
            [
                ("فرع حلب", "Aleppo Branch"),
                ("فرع إدلب", "Idlib Branch"),
            ]),
            ("المنطقة الساحلية", "Coastal Region",
            [
                ("فرع اللاذقية", "Latakia Branch"),
                ("فرع طرطوس", "Tartus Branch"),
            ]),
        };

        foreach (var (regionAr, regionEn, branches) in regionDefs)
        {
            var region = await _context.OrgUnit
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ParentId == hq.Id && x.NameAr == regionAr);

            if (region == null)
            {
                region = new OrgUnit
                {
                    ParentId = hq.Id,
                    NameAr = regionAr,
                    NameEn = regionEn,
                    Kind = OrgUnitKind.Region,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                await _context.OrgUnit.AddAsync(region);
                await _context.SaveChangesAsync();
            }
            else
            {
                region.Kind = OrgUnitKind.Region;
                region.ParentId = hq.Id;
            }

            foreach (var (branchAr, branchEn) in branches)
            {
                var branch = await _context.OrgUnit
                    .FirstOrDefaultAsync(x => !x.IsDeleted && x.NameAr == branchAr);

                if (branch == null)
                {
                    await _context.OrgUnit.AddAsync(new OrgUnit
                    {
                        ParentId = region.Id,
                        NameAr = branchAr,
                        NameEn = branchEn,
                        Kind = OrgUnitKind.Branch,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                    });
                }
                else
                {
                    branch.ParentId = region.Id;
                    branch.Kind = OrgUnitKind.Branch;
                }
            }
        }

        await MigrateLegacyBranchesAsync(hq.Id);
        await _context.SaveChangesAsync();
    }

    /// <summary>Re-parent legacy flat branches (فرع دمشق / حلب / الحجاز) under regions when present.</summary>
    private async Task MigrateLegacyBranchesAsync(string hqId)
    {
        var legacyMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["فرع دمشق"] = "المنطقة الوسطى",
            ["فرع الحجاز"] = "المنطقة الوسطى",
            ["فرع حلب"] = "المنطقة الشمالية",
        };

        var regions = await _context.OrgUnit
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Region)
            .ToListAsync();

        var legacyBranches = await _context.OrgUnit
            .Where(x => !x.IsDeleted && legacyMap.Keys.Contains(x.NameAr))
            .ToListAsync();

        foreach (var legacy in legacyBranches)
        {
            if (!legacyMap.TryGetValue(legacy.NameAr, out var regionName))
            {
                continue;
            }

            var region = regions.FirstOrDefault(r => r.NameAr == regionName);
            if (region == null)
            {
                continue;
            }

            legacy.ParentId = region.Id;
            legacy.Kind = OrgUnitKind.Branch;
        }
    }

    private async Task LinkDemoUsersAsync()
    {
        var users = await _context.Users
            .Where(u => u.Email != null && u.Email.Contains("syriatelecom-demo"))
            .ToListAsync();

        if (users.Count == 0)
        {
            return;
        }

        ApplicationUser? FindUser(string email) =>
            users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        var hq = await _context.OrgUnit
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Kind == OrgUnitKind.Headquarters);

        var branches = await _context.OrgUnit
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Branch)
            .ToListAsync();

        var regions = await _context.OrgUnit
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Region)
            .ToListAsync();

        OrgUnit? Branch(string nameAr) => branches.FirstOrDefault(b => b.NameAr == nameAr);
        OrgUnit? Region(string nameAr) => regions.FirstOrDefault(r => r.NameAr == nameAr);

        var changed = false;

        var misUser = FindUser("st-mis@syriatelecom-demo.local");
        if (hq != null && misUser != null)
        {
            if (string.IsNullOrEmpty(hq.ManagerUserId))
            {
                hq.ManagerUserId = misUser.Id;
                changed = true;
            }

            misUser.OrgUnitId = null;
            if (misUser.PrimaryMenuPersona == null)
            {
                misUser.PrimaryMenuPersona = Application.Common.Services.SecurityManager.TelecomMenuPersona.Executive;
            }
            await _userManager.UpdateAsync(misUser);
        }

        var regionalMappings = new (string Email, string RegionNameAr)[]
        {
            ("st-regional-north@syriatelecom-demo.local", "المنطقة الشمالية"),
            ("st-regional-central@syriatelecom-demo.local", "المنطقة الوسطى"),
            ("st-regional-coast@syriatelecom-demo.local", "المنطقة الساحلية"),
        };

        foreach (var (email, regionName) in regionalMappings)
        {
            var director = FindUser(email);
            var region = Region(regionName);
            if (director == null || region == null)
            {
                continue;
            }

            if (region.ManagerUserId != director.Id)
            {
                region.ManagerUserId = director.Id;
                changed = true;
            }

            if (!string.IsNullOrEmpty(director.OrgUnitId))
            {
                director.OrgUnitId = null;
                await _userManager.UpdateAsync(director);
            }
        }

        var branchManagerMap = new (string Email, string BranchNameAr)[]
        {
            ("st-branch-mezzeh@syriatelecom-demo.local", "فرع دمشق — المزة"),
            ("st-branch-aleppo@syriatelecom-demo.local", "فرع حلب"),
            ("st-branch-tartus@syriatelecom-demo.local", "فرع طرطوس"),
        };

        var opsBranchMap = new (string Email, string BranchNameAr)[]
        {
            ("st-showroom@syriatelecom-demo.local", "فرع دمشق — المزة"),
            ("st-callcenter@syriatelecom-demo.local", "فرع حلب"),
            ("st-backoffice@syriatelecom-demo.local", "فرع طرطوس"),
        };

        foreach (var (email, branchName) in branchManagerMap.Concat(opsBranchMap))
        {
            var user = FindUser(email);
            var branch = Branch(branchName);
            if (user == null || branch == null)
            {
                continue;
            }

            if (user.OrgUnitId != branch.Id)
            {
                user.OrgUnitId = branch.Id;
                await _userManager.UpdateAsync(user);
                changed = true;
            }

            if (email.StartsWith("st-branch-", StringComparison.OrdinalIgnoreCase)
                && branch.ManagerUserId != user.Id)
            {
                branch.ManagerUserId = user.Id;
                changed = true;
            }
        }

        if (changed)
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task LinkCustomersToBranchesAsync()
    {
        var branches = await _context.OrgUnit
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Branch)
            .ToListAsync();

        if (branches.Count == 0)
        {
            return;
        }

        var customers = await _context.Customer
            .Where(c => !c.IsDeleted && c.OrgUnitId == null)
            .ToListAsync();

        if (customers.Count == 0)
        {
            return;
        }

        var defaultBranch = branches.FirstOrDefault(b => b.NameAr.Contains("المزة", StringComparison.Ordinal))
            ?? branches[0];

        var cityBranch = new Dictionary<string, OrgUnit>(StringComparer.OrdinalIgnoreCase)
        {
            ["دمشق"] = branches.FirstOrDefault(b => b.NameAr.Contains("المزة", StringComparison.Ordinal)) ?? defaultBranch,
            ["حلب"] = branches.FirstOrDefault(b => b.NameAr.Contains("حلب", StringComparison.Ordinal)) ?? defaultBranch,
            ["اللاذقية"] = branches.FirstOrDefault(b => b.NameAr.Contains("اللاذقية", StringComparison.Ordinal)) ?? defaultBranch,
            ["طرطوس"] = branches.FirstOrDefault(b => b.NameAr.Contains("طرطوس", StringComparison.Ordinal)) ?? defaultBranch,
            ["إدلب"] = branches.FirstOrDefault(b => b.NameAr.Contains("إدلب", StringComparison.Ordinal)) ?? defaultBranch,
        };

        foreach (var customer in customers)
        {
            var city = customer.Address.City;
            if (!string.IsNullOrEmpty(city) && cityBranch.TryGetValue(city, out var branch))
            {
                customer.SetOrgUnitId(branch.Id);
            }
            else
            {
                customer.SetOrgUnitId(defaultBranch.Id);
            }
        }

        await _context.SaveChangesAsync();
    }
}
