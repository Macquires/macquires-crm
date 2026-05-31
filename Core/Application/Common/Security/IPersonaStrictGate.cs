using Application.Common.Services.SecurityManager;

namespace Application.Common.Security;

public interface IPersonaStrictGate
{
    Task<bool> IsStrictModeEnabledAsync(CancellationToken cancellationToken = default);
    bool IsPersonaAllowedForPath(TelecomMenuPersona persona, string requestPath);
    bool IsPersonaAllowedForCommand(TelecomMenuPersona persona, Type requestType);
}
