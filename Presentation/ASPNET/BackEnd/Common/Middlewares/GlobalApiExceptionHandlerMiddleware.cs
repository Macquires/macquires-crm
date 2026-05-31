using Microsoft.AspNetCore.Diagnostics;

namespace ASPNET.BackEnd.Common.Middlewares;

public class GlobalApiExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalApiExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, IExceptionHandler customExceptionHandler)
    {
        try
        {
            await _next(httpContext);

            // Middleware may have already written the body (e.g. StrictPersonaPath 403 HTML/JSON).
            if (!httpContext.Response.HasStarted)
            {
                switch (httpContext.Response.StatusCode)
                {
                    case StatusCodes.Status401Unauthorized:
                        await customExceptionHandler.TryHandleAsync(
                            httpContext,
                            new UnauthorizedAccessException("Unauthorized - Token missing or invalid"),
                            CancellationToken.None
                        );
                        break;

                    case StatusCodes.Status403Forbidden:
                        await customExceptionHandler.TryHandleAsync(
                            httpContext,
                            new Exception("Forbidden - Access denied"),
                            CancellationToken.None
                        );
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            await customExceptionHandler.TryHandleAsync(httpContext, ex, CancellationToken.None);
        }
    }
}
