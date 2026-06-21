using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace ASPNET.BackEnd.Common.Middlewares;

// Persona path matrix applies to Razor Pages only; APIs use [Authorize] + MediatR persona behaviors.

/// <summary>When strict persona mode is on, block HTTP paths outside the operator's persona navigation tree.</summary>
public class StrictPersonaPathMiddleware
{
    private static readonly PathString[] ExemptPrefixes =
    [
        new("/api/Security/Login"),
        new("/api/Security/RefreshToken"),
        new("/api/Security/Logout"),
        new("/api/Security/Register"),
        new("/Accounts"),
        new("/culture"),
        new("/swagger"),
        new("/hubs"),
    ];

    private readonly RequestDelegate _next;

    public StrictPersonaPathMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IPersonaStrictGate gate,
        IOperatorContext op)
    {
        if (!await gate.IsStrictModeEnabledAsync(context.RequestAborted))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (IsExempt(path))
        {
            await _next(context);
            return;
        }

        if (!op.IsAuthenticated || op.EffectivePersona == null)
        {
            await _next(context);
            return;
        }

        if (op.EffectivePersona == TelecomMenuPersona.SysAdmin)
        {
            await _next(context);
            return;
        }

        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!gate.IsPersonaAllowedForPath(op.EffectivePersona.Value, path))
        {
            var landing = TelecomPersonaLanding.ResolvePath(op.EffectivePersona);
            if (!string.IsNullOrEmpty(landing)
                && !string.Equals(NormalizePath(path), NormalizePath(landing), StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect(landing);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(
                """<!DOCTYPE html><html lang="ar" dir="rtl"><head><meta charset="utf-8"/><title>403</title></head><body style="font-family:sans-serif;text-align:center;padding:3rem"><h1>غير مصرح</h1><p>هذا المسار خارج صلاحيات Persona الخاصة بك.</p></body></html>""");
            return;
        }

        await _next(context);
    }

    private static bool IsExempt(string path)
    {
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix.Value!, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return path == "/" || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/FrontEnd", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/brand", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/noimage", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth_template", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/locales", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        var p = (path ?? "").Split('?')[0].TrimEnd('/');
        return p.Length == 0 ? "/" : p;
    }
}
