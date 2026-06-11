using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Application.Common.Security;
using Application.Common.Exceptions;
using System.Security.Claims;

namespace ASPNET.BackEnd.Common.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionAttribute(string permission)
    {
        _permission = permission;
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
        if (!await permissionEvaluator.HasPermissionAsync(userId, _permission))
        {
            throw new UnauthorizedPermissionException("Security Block: Missing specific granular system permission required for this operations pipeline.");
        }
    }
}
