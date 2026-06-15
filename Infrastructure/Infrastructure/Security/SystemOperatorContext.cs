using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace Infrastructure.Security;

/// <summary>Cross-branch system worker for hosted services (Operations_Manager RLS bypass, no HTTP context).</summary>
public sealed class SystemOperatorContext : IOperatorContext
{
    public const string SystemUserId = "system";

    public string? UserId => SystemUserId;

    public IReadOnlyList<string> Roles { get; } = [TelecomEnterpriseRoleMatrix.RoleOperationsManager];

    public TelecomMenuPersona? EffectivePersona => TelecomMenuPersona.SysAdmin;

    public string? BranchId => null;

    public bool IsAuthenticated => true;
}
