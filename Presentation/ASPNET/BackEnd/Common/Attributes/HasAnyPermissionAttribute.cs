using Application.Common.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ASPNET.BackEnd.Common.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasAnyPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissions;

    protected HasAnyPermissionAttribute(params string[] permissions)
    {
        _permissions = permissions;
    }

    protected HasAnyPermissionAttribute(IReadOnlyList<string> permissions)
        : this(permissions.ToArray())
    {
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var permissionEvaluator = context.HttpContext.RequestServices.GetRequiredService<IPermissionEvaluator>();
        foreach (var permission in _permissions)
        {
            if (await permissionEvaluator.HasPermissionAsync(userId, permission))
            {
                return;
            }
        }

        throw PermissionAuthorizationFailure.MissingAny(context, _permissions);
    }
}
