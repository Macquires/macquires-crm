namespace Application.Common.Security;

public interface IPermissionEvaluator
{
    Task<bool> HasPermissionAsync(string userId, string permissionKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserPermissionKeysAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserRoleNamesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default);
    Task UpdateRolePermissionsAsync(string roleName, IReadOnlyList<string> permissionKeys, string? grantedById, CancellationToken cancellationToken = default);
}
