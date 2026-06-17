using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Security;

/// <summary>Pure scope resolution for unit tests and <see cref="IStrategicDataScopeService"/>.</summary>
public static class StrategicDataScopeResolver
{
    public static StrategicDataScope Resolve(
        StrategicAccessLevel accessLevel,
        IReadOnlyList<OrgUnit> orgUnits,
        string? lockedRegionId,
        string? lockedBranchId,
        string? requestedRegionId,
        string? requestedBranchId)
    {
        var active = orgUnits.Where(x => x is { IsDeleted: false, IsActive: true }).ToList();
        var hq = active.FirstOrDefault(x => x.Kind == OrgUnitKind.Headquarters);
        var regions = active.Where(x => x.Kind == OrgUnitKind.Region).OrderBy(x => x.NameAr).ToList();
        var branches = active.Where(x => x.Kind == OrgUnitKind.Branch).OrderBy(x => x.NameAr).ToList();

        var regionOptions = regions
            .Select(r => new StrategicScopeOptionDto { Id = r.Id, NameAr = r.NameAr })
            .ToList();

        return accessLevel switch
        {
            StrategicAccessLevel.BranchManager => ResolveBranchManager(
                branches, lockedBranchId, regionOptions),
            StrategicAccessLevel.RegionalDirector => ResolveRegionalDirector(
                branches, regions, lockedRegionId, requestedBranchId, regionOptions),
            _ => ResolveGeneralManager(
                branches, regions, requestedRegionId, requestedBranchId, regionOptions, hq?.NameAr),
        };
    }

    public static StrategicAccessLevel DeriveAccessLevel(
        IReadOnlySet<string> permissions,
        string? userOrgUnitId,
        string? managedRegionId,
        string? managedHqId,
        OrgUnitKind? userOrgUnitKind)
    {
        if (PermissionScopeRules.HasNationalAdminScope(permissions))
        {
            return StrategicAccessLevel.GeneralManager;
        }

        if (managedHqId != null && PermissionScopeRules.HasExecutiveMisScope(permissions))
        {
            return StrategicAccessLevel.GeneralManager;
        }

        if (managedRegionId != null)
        {
            return StrategicAccessLevel.RegionalDirector;
        }

        if (!string.IsNullOrEmpty(userOrgUnitId) && userOrgUnitKind == OrgUnitKind.Branch)
        {
            return StrategicAccessLevel.BranchManager;
        }

        if (PermissionScopeRules.HasExecutiveMisScope(permissions) && string.IsNullOrEmpty(userOrgUnitId))
        {
            return StrategicAccessLevel.GeneralManager;
        }

        return StrategicAccessLevel.BranchManager;
    }

    private static StrategicDataScope ResolveGeneralManager(
        List<OrgUnit> branches,
        List<OrgUnit> regions,
        string? requestedRegionId,
        string? requestedBranchId,
        List<StrategicScopeOptionDto> regionOptions,
        string? hqName)
    {
        string? regionId = string.IsNullOrWhiteSpace(requestedRegionId) ? null : requestedRegionId.Trim();
        string? branchId = string.IsNullOrWhiteSpace(requestedBranchId) ? null : requestedBranchId.Trim();

        if (!string.IsNullOrEmpty(branchId))
        {
            var branch = branches.FirstOrDefault(b => b.Id == branchId)
                ?? throw new UnauthorizedAccessException("الفرع المطلوب غير موجود أو خارج صلاحياتك.");
            regionId = branch.ParentId;
        }
        else if (!string.IsNullOrEmpty(regionId)
            && regions.All(r => r.Id != regionId))
        {
            throw new UnauthorizedAccessException("المنطقة المطلوبة غير موجودة أو خارج صلاحياتك.");
        }

        var effectiveBranches = FilterBranches(branches, regionId, branchId);
        var branchOptions = BuildBranchOptions(branches, regionId);

        return new StrategicDataScope
        {
            AccessLevel = StrategicAccessLevel.GeneralManager,
            CanUseFilters = true,
            EffectiveRegionId = regionId,
            EffectiveBranchId = branchId,
            EffectiveBranchIds = effectiveBranches.Select(b => b.Id).ToList(),
            Regions = regionOptions,
            Branches = branchOptions,
            ScopeLabelAr = BuildScopeLabel(regions, effectiveBranches, regionId, branchId, hqName ?? "سوريا — كامل"),
        };
    }

    private static StrategicDataScope ResolveRegionalDirector(
        List<OrgUnit> branches,
        List<OrgUnit> regions,
        string? lockedRegionId,
        string? requestedBranchId,
        List<StrategicScopeOptionDto> regionOptions)
    {
        if (string.IsNullOrEmpty(lockedRegionId))
        {
            throw new UnauthorizedAccessException("لم يُعيَّن إقليم لهذا الحساب.");
        }

        var region = regions.FirstOrDefault(r => r.Id == lockedRegionId)
            ?? throw new UnauthorizedAccessException("الإقليم المعيّن غير موجود.");

        string? branchId = string.IsNullOrWhiteSpace(requestedBranchId) ? null : requestedBranchId.Trim();
        if (!string.IsNullOrEmpty(branchId))
        {
            var branch = branches.FirstOrDefault(b => b.Id == branchId && b.ParentId == lockedRegionId)
                ?? throw new UnauthorizedAccessException("الفرع المطلوب خارج إقليمك.");
        }

        var effectiveBranches = FilterBranches(branches, lockedRegionId, branchId);
        var branchOptions = BuildBranchOptions(branches, lockedRegionId);

        return new StrategicDataScope
        {
            AccessLevel = StrategicAccessLevel.RegionalDirector,
            CanUseFilters = false,
            EffectiveRegionId = lockedRegionId,
            EffectiveBranchId = branchId,
            EffectiveBranchIds = effectiveBranches.Select(b => b.Id).ToList(),
            Regions = regionOptions.Where(r => r.Id == lockedRegionId).ToList(),
            Branches = branchOptions,
            ScopeLabelAr = BuildScopeLabel(regions, effectiveBranches, lockedRegionId, branchId, region.NameAr),
        };
    }

    private static StrategicDataScope ResolveBranchManager(
        List<OrgUnit> branches,
        string? lockedBranchId,
        List<StrategicScopeOptionDto> regionOptions)
    {
        if (string.IsNullOrEmpty(lockedBranchId))
        {
            throw new UnauthorizedAccessException("لم يُعيَّن فرع لهذا الحساب.");
        }

        var branch = branches.FirstOrDefault(b => b.Id == lockedBranchId)
            ?? throw new UnauthorizedAccessException("الفرع المعيّن غير موجود.");

        return new StrategicDataScope
        {
            AccessLevel = StrategicAccessLevel.BranchManager,
            CanUseFilters = false,
            EffectiveRegionId = branch.ParentId,
            EffectiveBranchId = lockedBranchId,
            EffectiveBranchIds = [lockedBranchId],
            Regions = regionOptions.Where(r => r.Id == branch.ParentId).ToList(),
            Branches =
            [
                new StrategicScopeOptionDto
                {
                    Id = branch.Id,
                    NameAr = branch.NameAr,
                    RegionId = branch.ParentId,
                },
            ],
            ScopeLabelAr = branch.NameAr,
        };
    }

    private static List<OrgUnit> FilterBranches(List<OrgUnit> branches, string? regionId, string? branchId)
    {
        IEnumerable<OrgUnit> q = branches;
        if (!string.IsNullOrEmpty(regionId))
        {
            q = q.Where(b => b.ParentId == regionId);
        }

        if (!string.IsNullOrEmpty(branchId))
        {
            q = q.Where(b => b.Id == branchId);
        }

        return q.ToList();
    }

    private static List<StrategicScopeOptionDto> BuildBranchOptions(List<OrgUnit> branches, string? regionId)
    {
        var q = branches.AsEnumerable();
        if (!string.IsNullOrEmpty(regionId))
        {
            q = q.Where(b => b.ParentId == regionId);
        }

        return q.Select(b => new StrategicScopeOptionDto
        {
            Id = b.Id,
            NameAr = b.NameAr,
            RegionId = b.ParentId,
        }).ToList();
    }

    private static string BuildScopeLabel(
        List<OrgUnit> regions,
        List<OrgUnit> effectiveBranches,
        string? regionId,
        string? branchId,
        string fallback)
    {
        if (!string.IsNullOrEmpty(branchId))
        {
            return effectiveBranches.FirstOrDefault()?.NameAr ?? fallback;
        }

        if (!string.IsNullOrEmpty(regionId))
        {
            return regions.FirstOrDefault(r => r.Id == regionId)?.NameAr ?? fallback;
        }

        return fallback;
    }
}
