using System.Globalization;
using Application.Common.Exceptions;
using ASPNET.BackEnd.Common.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;

namespace ASPNET.BackEnd.Common.Handlers;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;
    private readonly IHostEnvironment _env;

    public CustomExceptionHandler(IHostEnvironment env)
    {
        _env = env;
        _exceptionHandlers = new()
            {
                { typeof(BusinessRuleViolationException), HandleBusinessRuleViolation },
                { typeof(TicketAlreadyClaimedException), HandleTicketAlreadyClaimed },
                { typeof(UnauthorizedPermissionException), HandleUnauthorizedPermission },
                { typeof(Exception), HandleException },
            };
    }

    private async Task HandleUnauthorizedPermission(HttpContext httpContext, Exception ex)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        var permissionEx = ex as UnauthorizedPermissionException;
        var diagnostic = permissionEx?.RequiredPermissions.Count > 0
            ? $"Endpoint: {permissionEx.Endpoint ?? "unknown"} | Required any of: {string.Join(", ", permissionEx.RequiredPermissions)}"
            : null;

        var result = new ApiErrorResult
        {
            Code = StatusCodes.Status403Forbidden,
            Message = _env.IsDevelopment() && diagnostic != null
                ? $"{ex.Message} ({diagnostic})"
                : ex.Message,
            MessageAr = "انتهاك أمني: ليس لديك الصلاحيات الكافية لتنفيذ هذا الإجراء.",
            MessageEn = "Security Violation: You do not possess the required compliance permissions to execute this action.",
            Error = new Error(
                _env.IsDevelopment() ? diagnostic : null,
                ex.Source,
                null,
                ex.GetType().Name)
        };

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(result);
    }

    private static async Task HandleTicketAlreadyClaimed(HttpContext httpContext, Exception ex)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        var messageAr = ex is BusinessRuleViolationException bre ? bre.MessageAr : ex.Message;
        var messageEn = ex is BusinessRuleViolationException breEn ? breEn.MessageEn : ex.Message;
        var result = new ApiErrorResult
        {
            Code = StatusCodes.Status409Conflict,
            Message = ResolveUserMessage(httpContext, messageAr, messageEn),
            MessageAr = messageAr,
            MessageEn = messageEn,
            Error = new Error(null, ex.Source, null, ex.GetType().Name)
        };

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(result);
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }
        else
        {
            await HandleException(httpContext, exception);
            return true;
        }

    }

    private static async Task HandleBusinessRuleViolation(HttpContext httpContext, Exception ex)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var messageAr = ex is BusinessRuleViolationException bre ? bre.MessageAr : ex.Message;
        var messageEn = ex is BusinessRuleViolationException breEn ? breEn.MessageEn : ex.Message;
        var result = new ApiErrorResult
        {
            Code = StatusCodes.Status400BadRequest,
            Message = ResolveUserMessage(httpContext, messageAr, messageEn),
            MessageAr = messageAr,
            MessageEn = messageEn,
            Error = new Error(null, ex.Source, null, ex.GetType().Name)
        };

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(result);
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var statusCode = httpContext.Response.StatusCode != 200
            ? httpContext.Response.StatusCode
            : StatusCodes.Status500InternalServerError;

        var isDev = _env.IsDevelopment();
        var errorMessage = isDev ? ex.Message : "An unexpected error occurred processing your request.";

        var result = new ApiErrorResult
        {
            Code = statusCode,
            Message = $"Exception: {errorMessage}",
            Error = new Error(
                isDev ? ex.InnerException?.Message : null,
                isDev ? ex.Source : null,
                isDev ? ex.StackTrace : null,
                ex.GetType().Name)
        };

        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(result);
    }

    private static string ResolveUserMessage(HttpContext httpContext, string messageAr, string messageEn)
    {
        var culture = httpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture
            ?? CultureInfo.CurrentUICulture;
        return string.Equals(culture.TwoLetterISOLanguageName, "ar", StringComparison.OrdinalIgnoreCase)
            ? messageAr
            : messageEn;
    }
}

