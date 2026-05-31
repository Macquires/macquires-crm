using Infrastructure.SecurityManager.Roles;
using System.Net;

namespace ASPNET.BackEnd.Common.Middlewares;

/// <summary>Server-side guard for admin Razor pages (Ghost Menu protection).</summary>
public class AdministrationPageGuardMiddleware
{
    private static readonly string[] ProtectedPrefixes =
    [
        "/Administration",
        "/Dashboards/DashboardWidgetList",
    ];

    private readonly RequestDelegate _next;

    public AdministrationPageGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (!IsProtectedPath(path))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(
                """<!DOCTYPE html><html lang="ar" dir="rtl"><head><meta charset="utf-8"/><title>403</title></head><body style="font-family:sans-serif;text-align:center;padding:3rem"><h1>غير مصرح</h1><p>يجب تسجيل الدخول كمسؤول نظام.</p><p><a href="/Accounts/Login">تسجيل الدخول</a></p></body></html>""");
            return;
        }

        if (!context.User.IsInRole(TelecomRoles.Admin))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(
                $"""<!DOCTYPE html><html lang="ar" dir="rtl"><head><meta charset="utf-8"/><title>403</title></head><body style="font-family:sans-serif;text-align:center;padding:3rem"><h1>غير مصرح</h1><p>هذه الصفحة متاحة لمسؤولي النظام ({WebUtility.HtmlEncode(TelecomRoles.Admin)}) فقط.</p></body></html>""");
            return;
        }

        await _next(context);
    }

    private static bool IsProtectedPath(string path)
    {
        foreach (var prefix in ProtectedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
