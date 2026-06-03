using Application.Common.Exceptions;
using Application.Common.Telecom.SellingLine;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class SellingLineEligibilityTests
{
    [Fact]
    public void MutationGuard_Blocks_Msisdn_Change_After_Completed()
    {
        var existing = new TelecomOperationRequest
        {
            Status = TelecomOperationStatus.Completed,
            MsisdnAssetId = "a1"
        };

        Assert.Throws<BusinessRuleViolationException>(() =>
            SellingLineOperationMutationGuard.EnsureEditableInventoryFields(
                existing, "a2", null, null, null));
    }

    [Fact]
    public void MutationGuard_Allows_Edit_While_Draft()
    {
        var existing = new TelecomOperationRequest
        {
            Status = TelecomOperationStatus.Draft,
            MsisdnAssetId = "a1"
        };

        SellingLineOperationMutationGuard.EnsureEditableInventoryFields(
            existing, "a2", null, null, null);
    }
}
