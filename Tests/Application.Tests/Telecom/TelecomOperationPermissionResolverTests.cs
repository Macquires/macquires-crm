using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public sealed class TelecomOperationPermissionResolverTests
{
    [Theory]
    [InlineData(TelecomOperationKind.ServiceModification, PermissionCatalog.TelecomVasToggle)]
    [InlineData(TelecomOperationKind.BadDebtRecovery, PermissionCatalog.TelecomLineCollection)]
    public void PermissionKeyForKind_maps_special_kinds(TelecomOperationKind kind, string expected) =>
        Assert.Equal(expected, TelecomOperationPermissionResolver.PermissionKeyForKind(kind));
}
