using Application.Common.Security;

namespace Application.Tests.Security;

public sealed class CustomerSupportPermissionSetsTests
{
    [Fact]
    public void CallCenter_DefaultGrants_AreSupportOnlyAgent()
    {
        var keys = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleCallCenter]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(CustomerSupportPermissionSets.IsSupportOnlyAgent(keys));
    }

    [Fact]
    public void Showroom_DefaultGrants_AreNotSupportOnlyAgent()
    {
        var keys = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleShowroom]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.False(CustomerSupportPermissionSets.IsSupportOnlyAgent(keys));
    }
}
