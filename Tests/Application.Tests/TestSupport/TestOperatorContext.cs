using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace Application.Tests.TestSupport;

public sealed class TestOperatorContext : IOperatorContext
{
    public static TestOperatorContext Instance { get; } = new();

    public string? UserId => "test-user";
    public IReadOnlyList<string> Roles { get; } = [];
    public TelecomMenuPersona? EffectivePersona => null;
    public bool IsAuthenticated => true;
    public string? BranchId => null;
}
