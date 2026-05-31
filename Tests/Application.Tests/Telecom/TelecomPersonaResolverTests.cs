using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;
using Infrastructure.SecurityManager.Roles;

namespace Application.Tests.Telecom;

public class TelecomPersonaResolverTests
{
    [Fact]
    public void ResolveFromRoles_prefers_SysAdmin_when_admin_and_management_present() =>
        Assert.Equal(
            TelecomMenuPersona.SysAdmin,
            TelecomPersonaResolver.ResolveFromRoles([TelecomRoles.Management, TelecomRoles.Admin]));

    [Fact]
    public void ResolveFromRoles_management_only_is_executive() =>
        Assert.Equal(
            TelecomMenuPersona.Executive,
            TelecomPersonaResolver.ResolveFromRoles([TelecomRoles.Management]));

    [Fact]
    public void ResolvePrimary_prefers_identity_roles_over_stored_persona() =>
        Assert.Equal(
            TelecomMenuPersona.CallCenter,
            TelecomPersonaResolver.ResolvePrimary(
                [TelecomRoles.CallCenter],
                TelecomMenuPersona.SysAdmin));
}
