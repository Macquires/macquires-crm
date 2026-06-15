using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace Infrastructure.DataAccessManager.EFCore;

internal sealed class DesignTimeOperatorContext : IOperatorContext
{
    public string? UserId => "design-time";
    public IReadOnlyList<string> Roles { get; } = [TelecomEnterpriseRoleMatrix.RoleAdmin];
    public TelecomMenuPersona? EffectivePersona => null;
    public string? BranchId => null;
    public bool IsAuthenticated => true;
}
