namespace Domain.Entities;

/// <summary>Granular permission grant for an Identity role (by role name).</summary>
public class RolePermission
{
    public string RoleName { get; set; } = null!;
    public string PermissionKey { get; set; } = null!;
    public DateTime GrantedAtUtc { get; set; }
    public string? GrantedById { get; set; }
}
