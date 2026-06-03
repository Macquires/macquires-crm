using Application.Common.Exceptions;
using Application.Common.Telecom.SimSwap;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class SimSwapEligibilityTests
{
    [Fact]
    public void ValidateDocumentsForConfirm_LostStolen_Requires_Identity()
    {
        var checker = new SimSwapEligibilityChecker(null!, null!);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.SimSwap,
            IsLostOrStolenReport = true,
            DocumentStatus = TelecomDocumentStatus.Missing,
            IdentityDocumentStorageKey = null,
        };

        var ex = Assert.Throws<BusinessRuleViolationException>(() => checker.ValidateDocumentsForConfirm(op));
        Assert.Contains("VAL-04-03", ex.Message);
    }

    [Fact]
    public void ValidateDocumentsForConfirm_Standard_Skips_Document_Gate()
    {
        var checker = new SimSwapEligibilityChecker(null!, null!);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.SimSwap,
            IsLostOrStolenReport = false,
            DocumentStatus = TelecomDocumentStatus.Missing,
        };

        checker.ValidateDocumentsForConfirm(op);
    }
}
