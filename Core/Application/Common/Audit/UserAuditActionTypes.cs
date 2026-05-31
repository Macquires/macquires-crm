namespace Application.Common.Audit;

/// <summary>Stable action codes for <see cref="Domain.Entities.UserAuditLog"/>.</summary>
public static class UserAuditActionTypes
{
    public const string UserLoggedIn = "UserLoggedIn";
    public const string UserLoginFailed = "UserLoginFailed";
    public const string UserLoggedOut = "UserLoggedOut";

    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserRolesUpdated = "UserRolesUpdated";

    public const string RolePermissionsUpdated = "RolePermissionsUpdated";
    public const string RolePermissionsCloned = "RolePermissionsCloned";

    public const string GlobalSettingsUpdated = "GlobalSettingsUpdated";

    public const string CustomerCreated = "CustomerCreated";
    public const string CustomerUpdated = "CustomerUpdated";

    public const string TelecomOperationConfirmed = "TelecomOperationConfirmed";

    public const string BulkImportStarted = "BulkImportStarted";
    public const string BulkImportExecuted = "BulkImportExecuted";

    public const string SubscriberSearched = "SubscriberSearched";
    public const string CustomerViewed = "CustomerViewed";
    public const string NetworkCommandExecuted = "NetworkCommandExecuted";
    public const string IntegrationCircuitBreakerChanged = "IntegrationCircuitBreakerChanged";
    public const string TicketResolved = "TicketResolved";
    public const string StrategicReportViewed = "StrategicReportViewed";
}
