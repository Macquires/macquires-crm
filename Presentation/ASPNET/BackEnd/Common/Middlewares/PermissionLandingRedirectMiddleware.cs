using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;

namespace ASPNET.BackEnd.Common.Middlewares;

/// <summary>
/// Redirects operators away from DefaultDashboard when their permission landing is elsewhere (e.g. BackOffice).
/// </summary>
public class PermissionLandingRedirectMiddleware
{
    private const string LegacyCrmDashboardPath = "/Dashboards/DefaultDashboard";

    private readonly RequestDelegate _next;

    public PermissionLandingRedirectMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IOperatorContext op,
        IPermissionEvaluator permissions)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            var userId = op.UserId;
            if (string.IsNullOrEmpty(userId)
                && context.Request.Cookies.TryGetValue("accessToken", out var cookieToken)
                && !string.IsNullOrWhiteSpace(cookieToken))
            {
                userId = TryReadUserIdFromJwt(cookieToken);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                var path = NormalizePath(context.Request.Path.Value);
                if (string.Equals(path, LegacyCrmDashboardPath, StringComparison.OrdinalIgnoreCase))
                {
                    var keys = await permissions.GetUserPermissionKeysAsync(userId, context.RequestAborted);
                    var permissionSet = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var roleNames = await permissions.GetUserRoleNamesAsync(userId, context.RequestAborted);
                    var persona = TelecomPersonaResolver.ResolvePrimary(roleNames);
                    var landing = TelecomWorkspaceRules.ResolveSessionLandingPath(
                        roleNames,
                        persona,
                        permissionSet);
                    if (!string.Equals(NormalizePath(landing), path, StringComparison.OrdinalIgnoreCase)
                        && !NavigationPermissionRules.IsNavUrlAllowed(path, permissionSet))
                    {
                        context.Response.Redirect(landing);
                        return;
                    }
                }
            }
        }

        await _next(context);
    }

    private static string NormalizePath(string? path)
    {
        var p = (path ?? "").Split('?')[0].TrimEnd('/');
        return p.Length == 0 ? "/" : p;
    }

    private static string? TryReadUserIdFromJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return null;
            }

            var payload = parts[1];
            var pad = payload.Length % 4;
            if (pad > 0)
            {
                payload += new string('=', 4 - pad);
            }

            var json = System.Text.Json.JsonDocument.Parse(
                Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')));
            if (json.RootElement.TryGetProperty("sub", out var sub))
            {
                return sub.GetString();
            }
        }
        catch
        {
            /* ignore malformed token */
        }

        return null;
    }
}
