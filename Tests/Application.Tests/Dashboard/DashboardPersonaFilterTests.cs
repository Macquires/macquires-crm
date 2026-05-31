using Application.Common.Dashboard;
using Application.Common.Services.SecurityManager;
using Infrastructure.SecurityManager.Roles;

namespace Application.Tests.Dashboard;

public class DashboardPersonaFilterTests
{
    [Theory]
    [InlineData("Executive,CallCenter", "Retail", false)]
    [InlineData("Retail", "Retail", true)]
    [InlineData("Executive, Retail", "Executive", true)]
    [InlineData("SysAdmin", "sysadmin", true)]
    public void WidgetVisibleForPersona_respects_csv(string allowed, string persona, bool expected) =>
        Assert.Equal(expected, DashboardPersonaFilter.WidgetVisibleForPersona(allowed, persona));

    [Fact]
    public void ResolveEffectivePersona_uses_preview_when_admin() =>
        Assert.Equal(
            TelecomMenuPersona.CallCenter,
            DashboardPersonaFilter.ResolveEffectivePersona(
                [TelecomRoles.Admin],
                "CallCenter"));

    [Fact]
    public void ResolveEffectivePersona_without_preview_uses_role_for_showroom() =>
        Assert.Equal(
            TelecomMenuPersona.Retail,
            DashboardPersonaFilter.ResolveEffectivePersona([TelecomRoles.Showroom], null));

    [Fact]
    public void ResolveEffectivePersona_maps_showroom_to_retail() =>
        Assert.Equal(
            TelecomMenuPersona.Retail,
            DashboardPersonaFilter.ResolveEffectivePersona([TelecomRoles.Showroom], null));
}
