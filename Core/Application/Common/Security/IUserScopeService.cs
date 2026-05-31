namespace Application.Common.Security;

/// <summary>Resolves which users/data the current operator may see (org hierarchy).</summary>
public interface IUserScopeService
{
    Task<bool> IsUnrestrictedAdminAsync(string userId, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetVisibleUserIdsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessUserAsync(string actorUserId, string targetUserId, CancellationToken cancellationToken = default);
}
