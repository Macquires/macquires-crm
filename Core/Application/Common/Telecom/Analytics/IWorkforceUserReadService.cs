namespace Application.Common.Telecom.Analytics;

public sealed class WorkforceUserSnapshot
{
    public string UserId { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string? OrgUnitId { get; init; }
    public string? OrgUnitNameAr { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public bool IsOnline { get; init; }
    public DateTime? LastActivityUtc { get; init; }
}

public interface IWorkforceUserReadService
{
    Task<IReadOnlyList<WorkforceUserSnapshot>> GetUsersInBranchesAsync(
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken = default);

    Task<int> CountOnlineInBranchesAsync(
        IReadOnlyList<string> branchIds,
        CancellationToken cancellationToken = default);
}
