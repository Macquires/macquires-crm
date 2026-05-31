using Application.Common.Exceptions;
using ASPNET.BackEnd.Common.Models;
using Microsoft.AspNetCore.Diagnostics;
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
                { typeof(Exception), HandleException },
            };
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
        var result = new ApiErrorResult
        {
            Code = StatusCodes.Status400BadRequest,
            Message = ex.Message,
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
}

