using Application.Common.Exceptions;
using Application.Common.Security;
using MediatR;

namespace Application.Common.Behaviors;

public class PersonaStrictAuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IOperatorContext _operator;
    private readonly IPersonaStrictGate _personaGate;

    public PersonaStrictAuthorizationBehaviour(IOperatorContext operatorContext, IPersonaStrictGate personaGate)
    {
        _operator = operatorContext;
        _personaGate = personaGate;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!await _personaGate.IsStrictModeEnabledAsync(cancellationToken))
        {
            return await next();
        }

        if (!_operator.IsAuthenticated || _operator.EffectivePersona == null)
        {
            return await next();
        }

        var allowed = PersonaCommandAccessRules.GetAllowedPersonas(typeof(TRequest));
        if (allowed == null || allowed.Count == 0)
        {
            return await next();
        }

        if (!_personaGate.IsPersonaAllowedForCommand(_operator.EffectivePersona.Value, typeof(TRequest)))
        {
            throw new BusinessRuleViolationException(
                $"وضع Persona الصارم: العملية غير مسموحة لشخصيتك ({_operator.EffectivePersona}).");
        }

        return await next();
    }
}
