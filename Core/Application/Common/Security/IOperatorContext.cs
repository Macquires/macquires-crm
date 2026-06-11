using Application.Common.Services.SecurityManager;

namespace Application.Common.Security;

/// <summary>Current HTTP operator (set per request in Infrastructure).</summary>
public interface IOperatorContext
{
    string? UserId { get; }
    IReadOnlyList<string> Roles { get; }
    TelecomMenuPersona? EffectivePersona { get; }
    string? BranchId { get; }
    bool IsAuthenticated { get; }
}
