using Domain.Enums;

namespace Application.Common.Security;

public static class TelecomOperationPermissionResolver
{
    public static string PermissionKeyForKind(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.Migration => PermissionCatalog.TelecomLineMigrate,
        TelecomOperationKind.SimSwap => PermissionCatalog.TelecomLineSimSwap,
        TelecomOperationKind.NewActivation => PermissionCatalog.TelecomLineActivate,
        TelecomOperationKind.TakeOver => PermissionCatalog.TelecomLineActivate,
        TelecomOperationKind.NumberPortability => PermissionCatalog.TelecomLineActivate,
        _ => PermissionCatalog.TelecomLineActivate,
    };
}
