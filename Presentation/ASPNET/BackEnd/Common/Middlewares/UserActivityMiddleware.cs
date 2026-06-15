using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace ASPNET.BackEnd.Common.Middlewares;

/// <summary>Throttled update of <c>LastActivityAtUtc</c> for authenticated users.</summary>
public class UserActivityMiddleware
{
    private static readonly TimeSpan Throttle = TimeSpan.FromMinutes(2);
    private readonly RequestDelegate _next;

    public UserActivityMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        DataContext db,
        IMemoryCache cache)
    {
        await _next(context);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var cacheKey = $"UserActivity:{userId}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            return;
        }

        cache.Set(cacheKey, true, Throttle);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, context.RequestAborted);
        if (user == null)
        {
            return;
        }

        user.LastActivityAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.RequestAborted);
    }
}
