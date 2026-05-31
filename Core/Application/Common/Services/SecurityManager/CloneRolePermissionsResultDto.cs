namespace Application.Common.Services.SecurityManager;

public class CloneRolePermissionsResultDto
{
    public string? NewRoleName { get; init; }
    public IReadOnlyList<string>? PermissionKeys { get; init; }
}
