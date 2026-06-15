using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

internal static class OperationConfirmValidationHelpers
{
    public static bool HasKycProofForActivation(TelecomOperationRequest entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.KycDocumentReferenceId))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(entity.OverrideReasonCode))
        {
            return true;
        }

        if (entity.KycVerifiedAtUtc.HasValue)
        {
            return true;
        }

        if (entity.DocumentStatus is TelecomDocumentStatus.Uploaded or TelecomDocumentStatus.Verified)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(entity.IdentityDocumentStorageKey);
    }

    public static void ApplyFraudClearanceIfRequired(
        TelecomOperationRequest entity,
        string? actorUserId)
    {
        if (!string.Equals(entity.ApprovalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
            || entity.FraudClearanceConfirmed
            || string.IsNullOrWhiteSpace(actorUserId))
        {
            return;
        }

        entity.FraudClearanceConfirmed = true;
        entity.FraudClearanceByUserId = actorUserId;
    }
}
