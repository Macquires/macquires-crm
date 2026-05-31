namespace Application.Common.Services.SecurityManager;
public record GetUserListResultDto
{
    public string? Id { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool? EmailConfirmed { get; init; }
    public bool? IsBlocked { get; init; }
    public bool? IsDeleted { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? PrimaryMenuPersona { get; init; }
    public string? ManagerUserId { get; init; }
    public string? ManagerDisplayName { get; init; }
    public string? OrgUnitId { get; init; }
    public string? OrgUnitNameAr { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public DateTime? LastActivityAtUtc { get; init; }
    public string? RolesDisplay { get; init; }
    public IReadOnlyList<string>? Roles { get; init; }
    public bool IsOnline { get; init; }
}

