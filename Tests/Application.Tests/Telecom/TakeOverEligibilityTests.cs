using Application.Common.Exceptions;
using Application.Common.Telecom.TakeOver;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class TakeOverEligibilityTests
{
    [Fact]
    public void ValidateDocumentsForConfirm_Requires_Identity_Upload()
    {
        var checker = new TakeOverEligibilityChecker(null!, null!);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.TakeOver,
            DocumentStatus = TelecomDocumentStatus.Missing,
            IdentityDocumentStorageKey = null,
        };

        var ex = Assert.Throws<BusinessRuleViolationException>(() => checker.ValidateDocumentsForConfirm(op));
        Assert.Contains("VAL-07-01", ex.Message);
    }

    [Fact]
    public void ValidateDocumentsForConfirm_Allows_Uploaded_Document()
    {
        var checker = new TakeOverEligibilityChecker(null!, null!);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.TakeOver,
            DocumentStatus = TelecomDocumentStatus.Uploaded,
            IdentityDocumentStorageKey = "telecom-ops/demo.pdf",
        };

        checker.ValidateDocumentsForConfirm(op);
    }
}
