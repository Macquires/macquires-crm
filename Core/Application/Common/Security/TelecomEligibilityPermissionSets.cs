namespace Application.Common.Security;

public static class TelecomEligibilityPermissionSets
{
    public static readonly IReadOnlyList<string> ReconnectAny =
    [
        PermissionCatalog.TelecomLineReconnect,
        PermissionCatalog.TelecomLineReconnectRequest,
        PermissionCatalog.TelecomCustomerProvisioning,
    ];

    public static readonly IReadOnlyList<string> BadDebtAny =
    [
        PermissionCatalog.FinanceBdrView,
        PermissionCatalog.FinanceBdrExecute,
        PermissionCatalog.TelecomLineCollection,
        PermissionCatalog.TelecomLineCollectionRequest,
    ];
}
