using Application.Common.Security;
using Application.Common.Telecom.Confirm;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Telecom;

public sealed class TelecomOperationConfirmPermissionResolverTests
{
    [Fact]
    public void BackOfficeTermination_YieldsApproveBeforeRequest()
    {
        var operation = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.Termination,
            ApprovalLevelRequired = "BackOffice",
        };

        var keys = TelecomOperationConfirmPermissionResolver.ResolvePermissionKeys(operation).ToList();

        Assert.Equal(2, keys.Count);
        Assert.Equal(PermissionCatalog.TelecomLineTerminationApprove, keys[0]);
        Assert.Equal(PermissionCatalog.TelecomLineTermination, keys[1]);
    }

    [Fact]
    public void LostOrStolenSimSwap_YieldsApproveFirst()
    {
        var operation = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.SimSwap,
            IsLostOrStolenReport = true,
        };

        var keys = TelecomOperationConfirmPermissionResolver.ResolvePermissionKeys(operation).ToList();

        Assert.Equal(PermissionCatalog.TelecomLineSimSwapApprove, keys[0]);
        Assert.Equal(PermissionCatalog.TelecomLineSimSwap, keys[1]);
    }

    [Fact]
    public void PlainMigration_FallsBackToKindPermission()
    {
        var operation = new TelecomOperationRequest { Kind = TelecomOperationKind.Migration };

        var keys = TelecomOperationConfirmPermissionResolver.ResolvePermissionKeys(operation).ToList();

        Assert.Single(keys);
        Assert.Equal(PermissionCatalog.TelecomLineMigrate, keys[0]);
    }

    [Fact]
    public void BackOfficeBadDebt_YieldsCollectionApproveFirst()
    {
        var operation = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.BadDebtRecovery,
            ApprovalLevelRequired = "BackOffice",
        };

        var keys = TelecomOperationConfirmPermissionResolver.ResolvePermissionKeys(operation).ToList();

        Assert.Equal(PermissionCatalog.TelecomLineCollectionApprove, keys[0]);
        Assert.Equal(PermissionCatalog.TelecomLineCollection, keys[1]);
    }
}
