using Application.Common.Security;

namespace Application.Tests.Security;

public class PermissionLandingResolverTests
{
    [Theory]
    [InlineData(new[] { PermissionCatalog.AdminUsersManage }, PermissionLandingResolver.AdministrationUsers)]
    [InlineData(new[] { PermissionCatalog.BulkImportUpload }, PermissionLandingResolver.BackOfficeDashboard)]
    [InlineData(new[] { PermissionCatalog.TelecomAssetManage }, PermissionLandingResolver.BackOfficeDashboard)]
    [InlineData(new[] { PermissionCatalog.CustomerView }, PermissionLandingResolver.DefaultCrmDashboard)]
    [InlineData(new[] { PermissionCatalog.BulkImportUpload, PermissionCatalog.CustomerView }, PermissionLandingResolver.BackOfficeDashboard)]
    public void Resolve_uses_priority_matrix(string[] keys, string expected) =>
        Assert.Equal(expected, PermissionLandingResolver.Resolve(keys));

    [Fact]
    public void Resolve_empty_permissions_falls_back_to_profile() =>
        Assert.Equal(PermissionLandingResolver.MyProfile, PermissionLandingResolver.Resolve(Array.Empty<string>()));
}
