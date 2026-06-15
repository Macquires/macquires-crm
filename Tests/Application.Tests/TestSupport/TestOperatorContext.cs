using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace Application.Tests.TestSupport;

public sealed class TestOperatorContext : IOperatorContext
{
    public const string DefaultBranchId = "test-branch-1";

    public static TestOperatorContext Instance { get; } = new();

    public static IOperatorContext ForBranch(string branchId) =>
        new BranchScopedOperatorContext(branchId);

    public string? UserId => "test-user";
    public IReadOnlyList<string> Roles { get; } = [TelecomEnterpriseRoleMatrix.RoleAdmin];
    public TelecomMenuPersona? EffectivePersona => null;
    public bool IsAuthenticated => true;
    public string? BranchId => DefaultBranchId;

    private sealed class BranchScopedOperatorContext : IOperatorContext
    {
        public BranchScopedOperatorContext(string branchId) => BranchId = branchId;

        public string? UserId => "branch-user";
        public IReadOnlyList<string> Roles { get; } = [TelecomEnterpriseRoleMatrix.RoleCallCenter];
        public TelecomMenuPersona? EffectivePersona => null;
        public bool IsAuthenticated => true;
        public string? BranchId { get; }
    }
}
