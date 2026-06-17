using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace Infrastructure.Security;

/// <summary>Admin operator used during database seeding (no HTTP context / branch RLS).</summary>
public sealed class SeedOperatorContext : IOperatorContext
{
    public string? UserId => "seed";

    public IReadOnlyList<string> Roles { get; } = [TelecomEnterpriseRoleMatrix.RoleAdmin];

    public IReadOnlyList<string> Permissions { get; } =
        PermissionCatalog.All.Select(p => p.Key).ToList();

    public TelecomMenuPersona? EffectivePersona => TelecomMenuPersona.SysAdmin;

    public string? BranchId => null;

    public bool IsAuthenticated => true;
}
