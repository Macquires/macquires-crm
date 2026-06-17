using Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ASPNET.BackEnd.Common.Attributes;

internal static class PermissionAuthorizationFailure
{
    public static UnauthorizedPermissionException MissingAny(
        AuthorizationFilterContext context,
        IReadOnlyList<string> requiredAny)
    {
        var endpoint = DescribeEndpoint(context);
        return new UnauthorizedPermissionException(
            $"Security Block: Missing granular system permission for {endpoint}. Required any of: {string.Join(", ", requiredAny)}",
            requiredAny,
            endpoint);
    }

    public static UnauthorizedPermissionException Missing(
        AuthorizationFilterContext context,
        string required)
    {
        var endpoint = DescribeEndpoint(context);
        return new UnauthorizedPermissionException(
            $"Security Block: Missing permission '{required}' for {endpoint}.",
            [required],
            endpoint);
    }

    private static string DescribeEndpoint(AuthorizationFilterContext context)
    {
        var path = context.HttpContext.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return context.HttpContext.Request.Method;
        }

        return $"{context.HttpContext.Request.Method} {path}";
    }
}
