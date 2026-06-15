using Application.Common.Security;

using Microsoft.AspNetCore.Authorization;

namespace ASPNET.BackEnd.Common.Attributes;

/// <summary>Any authenticated operator (self-service profile, session, menu badges).</summary>
public sealed class RequireAuthenticatedOperatorAttribute : AuthorizeAttribute
{
    public RequireAuthenticatedOperatorAttribute()
    {
    }
}

public sealed class RequireAdminUsersManageAttribute : HasAnyPermissionAttribute
{
    public RequireAdminUsersManageAttribute()
        : base(AdminPermissionSets.UsersManageAny)
    {
    }
}

public sealed class RequireAdminRolesManageAttribute : HasAnyPermissionAttribute
{
    public RequireAdminRolesManageAttribute()
        : base(AdminPermissionSets.RolesManageAny)
    {
    }
}

public sealed class RequireAdminSettingsManageAttribute : HasAnyPermissionAttribute
{
    public RequireAdminSettingsManageAttribute()
        : base(AdminPermissionSets.SettingsManageAny)
    {
    }
}

public sealed class RequireAdminAuditViewAttribute : HasAnyPermissionAttribute
{
    public RequireAdminAuditViewAttribute()
        : base(AdminPermissionSets.AuditViewAny)
    {
    }
}
