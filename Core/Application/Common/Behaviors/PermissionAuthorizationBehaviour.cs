using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using MediatR;

namespace Application.Common.Behaviors;

public class PermissionAuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IOperatorContext _operator;
    private readonly IPermissionEvaluator _permissions;

    public PermissionAuthorizationBehaviour(IOperatorContext operatorContext, IPermissionEvaluator permissions)
    {
        _operator = operatorContext;
        _permissions = permissions;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequireAnyPermission anyPerm && anyPerm.PermissionKeys.Count > 0)
        {
            if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
            {
                throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
            }

            foreach (var key in anyPerm.PermissionKeys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (await _permissions.HasPermissionAsync(_operator.UserId, key, cancellationToken))
                {
                    return await next();
                }
            }

            throw new BusinessRuleViolationException("ليس لديك صلاحية لتنفيذ هذه العملية.");
        }

        if (request is IOperationalKpiRequest)
        {
            if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
            {
                throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
            }

            foreach (var key in OperationalKpiPermissionSets.ReadAny)
            {
                if (await _permissions.HasPermissionAsync(_operator.UserId, key, cancellationToken))
                {
                    return await next();
                }
            }

            throw new BusinessRuleViolationException("ليس لديك صلاحية لتنفيذ هذه العملية.");
        }

        if (request is IRequireAuthenticatedOperator)
        {
            if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
            {
                throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
            }

            return await next();
        }

        if (request is not IRequirePermission secured || string.IsNullOrWhiteSpace(secured.PermissionKey))
        {
            return await next();
        }

        if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
        }

        if (!await _permissions.HasPermissionAsync(_operator.UserId, secured.PermissionKey, cancellationToken))
        {
            throw new BusinessRuleViolationException("ليس لديك صلاحية لتنفيذ هذه العملية.");
        }

        return await next();
    }
}
