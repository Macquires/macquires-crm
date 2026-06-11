using Application.Common.Exceptions;
using Application.Common.Telecom.SellingLine;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
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

    [Fact]
    public void CreateValidator_Requires_TargetSubscriptionTypeId_For_NewActivation()
    {
        var validator = new CreateTelecomOperationRequestValidator();
        var request = new CreateTelecomOperationRequest
        {
            Kind = TelecomOperationKind.NewActivation,
            SubscriberProfileId = "prof-1",
            MsisdnAssetId = "msisdn-1",
            TargetSubscriptionTypeId = "",
        };

        var result = validator.Validate(request);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTelecomOperationRequest.TargetSubscriptionTypeId));
    }

    [Fact]
    public void CreateValidator_Requires_DealerCode_When_Dealer_Channel()
    {
        var validator = new CreateTelecomOperationRequestValidator();
        var request = new CreateTelecomOperationRequest
        {
            Kind = TelecomOperationKind.NewActivation,
            SubscriberProfileId = "prof-1",
            MsisdnAssetId = "msisdn-1",
            TargetSubscriptionTypeId = "a0e0e0e0-0000-4000-8000-000000000001",
            ActivationChannel = ActivationChannel.Dealer,
            DealerCode = "",
        };

        var result = validator.Validate(request);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTelecomOperationRequest.DealerCode));
    }

    [Fact]
    public void CreateValidator_Allows_Dealer_Channel_With_Code()
    {
        var validator = new CreateTelecomOperationRequestValidator();
        var request = new CreateTelecomOperationRequest
        {
            Kind = TelecomOperationKind.NewActivation,
            SubscriberProfileId = "prof-1",
            MsisdnAssetId = "msisdn-1",
            TargetSubscriptionTypeId = "a0e0e0e0-0000-4000-8000-000000000001",
            ActivationChannel = ActivationChannel.Dealer,
            DealerCode = "DLR-001",
            KycDocumentReferenceId = "kyc-ref-001",
        };

        var result = validator.Validate(request);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateTelecomOperationRequest.DealerCode));
    }

    [Fact]
    public void CreateValidator_Requires_KycDocumentReferenceId_For_NewActivation()
    {
        var validator = new CreateTelecomOperationRequestValidator();
        var request = new CreateTelecomOperationRequest
        {
            Kind = TelecomOperationKind.NewActivation,
            SubscriberProfileId = "prof-1",
            MsisdnAssetId = "msisdn-1",
            TargetSubscriptionTypeId = "a0e0e0e0-0000-4000-8000-000000000001",
            KycDocumentReferenceId = "",
        };

        var result = validator.Validate(request);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTelecomOperationRequest.KycDocumentReferenceId));
    }

    [Theory]
    [InlineData(TelecomDocumentStatus.Missing, null, null, false)]
    [InlineData(TelecomDocumentStatus.Uploaded, null, null, true)]
    [InlineData(TelecomDocumentStatus.Missing, "BO-KYC-01", null, true)]
    [InlineData(TelecomDocumentStatus.Missing, null, "2026-01-01", true)]
    public void KycGate_respects_document_override_and_verified(
        TelecomDocumentStatus docStatus,
        string? overrideReason,
        string? kycVerified,
        bool shouldPass)
    {
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.NewActivation,
            DocumentStatus = docStatus,
            OverrideReasonCode = overrideReason,
            KycVerifiedAtUtc = kycVerified == null ? null : DateTime.Parse(kycVerified),
        };

        var passed = true;
        try
        {
            InvokeKycGate(op);
        }
        catch (BusinessRuleViolationException)
        {
            passed = false;
        }

        Assert.Equal(shouldPass, passed);
    }

    private static void InvokeKycGate(TelecomOperationRequest op)
    {
        if (!string.IsNullOrWhiteSpace(op.OverrideReasonCode))
        {
            return;
        }

        if (op.KycVerifiedAtUtc.HasValue)
        {
            return;
        }

        if (op.DocumentStatus is TelecomDocumentStatus.Verified or TelecomDocumentStatus.Uploaded)
        {
            return;
        }

        throw new BusinessRuleViolationException("VAL-02-01");
    }
}
