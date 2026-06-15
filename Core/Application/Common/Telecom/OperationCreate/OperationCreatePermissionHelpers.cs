using Application.Common.Exceptions;
using Application.Common.Security;

namespace Application.Common.Telecom.OperationCreate;

public static class OperationCreatePermissionHelpers
{
    public static async Task EnsureAnyPermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        string loginMessageAr,
        string deniedMessageAr,
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException(loginMessageAr);
        }

        foreach (var key in keys)
        {
            if (await permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(deniedMessageAr);
    }

    public static Task EnsureTakeOverCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب نقل الملكية.",
            "ليس لديك صلاحية إنشاء طلب نقل الملكية (telecom.line.transfer_request).",
            [
                PermissionCatalog.TelecomLineTransferRequest,
                PermissionCatalog.TelecomLineTransferOwnership,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);

    public static Task EnsureSimSwapCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب تبديل الشريحة.",
            "ليس لديك صلاحية إنشاء طلب تبديل الشريحة (telecom.line.simswap_request).",
            [
                PermissionCatalog.TelecomLineSimSwapRequest,
                PermissionCatalog.TelecomLineSimSwap,
                PermissionCatalog.TelecomLineSimSwapApprove,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);

    public static Task EnsureChangeNumberCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب تغيير الرقم.",
            "ليس لديك صلاحية إنشاء طلب تغيير الرقم (telecom.line.changenumber_request).",
            [
                PermissionCatalog.TelecomLineChangeNumberRequest,
                PermissionCatalog.TelecomLineChangeNumber,
                PermissionCatalog.TelecomLineChangeNumberApprove,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);

    public static Task EnsureTerminationCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب إنهاء الخط.",
            "ليس لديك صلاحية إنشاء طلب إنهاء الخط (telecom.line.termination_request).",
            [
                PermissionCatalog.TelecomLineTerminationRequest,
                PermissionCatalog.TelecomLineTermination,
                PermissionCatalog.TelecomLineTerminationApprove,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);

    public static Task EnsureSuspensionCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب حظر الخط.",
            "ليس لديك صلاحية إنشاء طلب حظر الخط (telecom.line.suspension_request).",
            [
                PermissionCatalog.TelecomLineSuspensionRequest,
                PermissionCatalog.TelecomLineSuspension,
                PermissionCatalog.TelecomLineSuspensionApprove,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);

    public static Task EnsureReconnectCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب إعادة التفعيل.",
            "ليس لديك صلاحية إنشاء طلب إعادة التفعيل (telecom.line.reconnect_request).",
            [
                PermissionCatalog.TelecomLineReconnectRequest,
                PermissionCatalog.TelecomLineReconnect,
                PermissionCatalog.TelecomLineReconnectApprove,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);

    public static Task EnsureRefundCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب استرداد مالي.",
            "ليس لديك صلاحية إنشاء طلب الاسترداد (telecom.line.refund_request).",
            [
                PermissionCatalog.TelecomLineRefundRequest,
                PermissionCatalog.TelecomLineRefund,
                PermissionCatalog.TelecomLineRefundApprove,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);

    public static Task EnsureBadDebtCreatePermissionAsync(
        string? userId,
        IPermissionEvaluator permissions,
        CancellationToken cancellationToken) =>
        EnsureAnyPermissionAsync(
            userId,
            permissions,
            "يجب تسجيل الدخول لإنشاء طلب التحصيل.",
            "ليس لديك صلاحية إنشاء طلب التحصيل (telecom.line.collection_request).",
            [
                PermissionCatalog.TelecomLineCollectionRequest,
                PermissionCatalog.TelecomLineCollection,
                PermissionCatalog.TelecomLineCollectionApprove,
                PermissionCatalog.TelecomLineCollectionManage,
                PermissionCatalog.CustomerUpdate,
                PermissionCatalog.TelecomCustomerProvisioning,
            ],
            cancellationToken);
}
