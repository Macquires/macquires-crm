using Application.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.DeviceSales;

public static class DeviceSaleOperationMutationGuard
{
    public static void EnsureEditable(TelecomOperationRequest operation)
    {
        if (operation.Kind != TelecomOperationKind.DeviceSale)
        {
            return;
        }

        if (operation.Status is TelecomOperationStatus.Completed or TelecomOperationStatus.Failed)
        {
            throw new BusinessRuleViolationException(
                "لا يمكن تعديل بيع جهاز بعد الإكمال أو الفشل (IMEI والخطة مقفولة).");
        }
    }
}
