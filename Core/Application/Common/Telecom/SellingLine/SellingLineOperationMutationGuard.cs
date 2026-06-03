using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.SellingLine;

/// <summary>Locks MSISDN/SIM/Offer after fulfillment (Blueprint field editability).</summary>
public static class SellingLineOperationMutationGuard
{
    public static void EnsureEditableInventoryFields(
        TelecomOperationRequest existing,
        string? msisdnAssetId,
        string? simInventoryId,
        string? productOfferingId,
        string? productId)
    {
        if (!IsTerminal(existing.Status))
        {
            return;
        }

        if (FieldChanged(existing.MsisdnAssetId, msisdnAssetId)
            || FieldChanged(existing.SimInventoryId, simInventoryId)
            || FieldChanged(existing.ProductOfferingId, productOfferingId)
            || FieldChanged(existing.ProductId, productId))
        {
            throw new BusinessRuleViolationException(
                "لا يمكن تعديل الرقم أو الشريحة أو العرض بعد اكتمال أو فشل العملية.");
        }
    }

    private static bool IsTerminal(TelecomOperationStatus status) =>
        status is TelecomOperationStatus.Completed or TelecomOperationStatus.Failed;

    private static bool FieldChanged(string? before, string? after)
    {
        var b = (before ?? string.Empty).Trim();
        var a = (after ?? string.Empty).Trim();
        return !string.Equals(b, a, StringComparison.Ordinal);
    }
}
