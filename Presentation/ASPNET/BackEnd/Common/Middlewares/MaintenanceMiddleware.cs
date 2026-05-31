using Application.Common.Settings;
using Infrastructure.SecurityManager.Roles;
using System.Net;

namespace ASPNET.BackEnd.Common.Middlewares;

public class MaintenanceMiddleware
{
    private static readonly PathString[] AllowedPaths =
    [
        new("/api/Security/Login"),
        new("/api/Security/RefreshToken"),
        new("/Accounts/Login"),
        new("/culture/set"),
    ];

    private readonly RequestDelegate _next;

    public MaintenanceMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IGlobalSettingsProvider settings)
    {
        if (!await settings.GetBoolAsync(GlobalSettingKeys.MaintenanceMode))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (AllowedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (context.User.IsInRole(TelecomRoles.Admin))
        {
            await _next(context);
            return;
        }

        if (await IsBypassIpAsync(context, settings))
        {
            await _next(context);
            return;
        }

        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                code = 503,
                message = await settings.GetValueAsync(GlobalSettingKeys.MaintenanceMessageAr)
                    ?? "Maintenance mode",
            });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        var msgAr = await settings.GetValueAsync(GlobalSettingKeys.MaintenanceMessageAr)
            ?? "النظام قيد الصيانة";
        var msgEn = await settings.GetValueAsync(GlobalSettingKeys.MaintenanceMessageEn)
            ?? "System under maintenance";
        await context.Response.WriteAsync(
            $"""<!DOCTYPE html><html lang="ar" dir="rtl"><head><meta charset="utf-8"/><title>صيانة</title></head><body style="font-family:sans-serif;text-align:center;padding:3rem"><h1>{WebUtility.HtmlEncode(msgAr)}</h1><p>{WebUtility.HtmlEncode(msgEn)}</p></body></html>""");
    }

    private static async Task<bool> IsBypassIpAsync(HttpContext context, IGlobalSettingsProvider settings)
    {
        var clientIp = ResolveClientIp(context);
        if (string.IsNullOrEmpty(clientIp))
        {
            return false;
        }

        var allowed = await settings.GetCsvListAsync(GlobalSettingKeys.MaintenanceBypassIPs);
        return allowed.Any(ip => string.Equals(ip, clientIp, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.MapToIPv4().ToString()
            ?? context.Connection.RemoteIpAddress?.ToString();
    }
}
