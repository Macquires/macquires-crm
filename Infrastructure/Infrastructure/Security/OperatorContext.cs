using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Security;

public class OperatorContext : IOperatorContext
{
    private readonly IHttpContextAccessor _http;

    public OperatorContext(IHttpContextAccessor http) => _http = http;

    public string? UserId =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public IReadOnlyList<string> Roles =>
        _http.HttpContext?.User?
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList()
        ?? [];

    public TelecomMenuPersona? EffectivePersona =>
        _http.HttpContext?.Items.TryGetValue(OperatorContextKeys.EffectivePersona, out var p) == true && p is TelecomMenuPersona persona
            ? persona
            : null;

    public bool IsAuthenticated =>
        _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public string? BranchId
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user is null)
            {
                return null;
            }

            return user.FindFirstValue(TelecomAuthClaims.BranchId)
                ?? user.FindFirstValue("BranchId");
        }
    }
}

public static class OperatorContextKeys
{
    public const string EffectivePersona = "EffectivePersona";
}

public class OperatorContextMiddleware
{
    private readonly RequestDelegate _next;

    public OperatorContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, DataContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            TelecomMenuPersona? persona = null;

            if (TryResolvePreviewPersona(context, roles, out var preview))
            {
                persona = preview;
            }
            else if (TryResolvePersonaFromClaims(context.User, out var fromClaim))
            {
                persona = fromClaim;
            }
            else
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var stored = await db.Users.AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.PrimaryMenuPersona })
                        .FirstOrDefaultAsync(context.RequestAborted);

                    persona = TelecomPersonaResolver.ResolvePrimary(roles, stored?.PrimaryMenuPersona);
                }
            }

            if (persona.HasValue)
            {
                context.Items[OperatorContextKeys.EffectivePersona] = persona.Value;
            }
        }

        await _next(context);
    }

    private static bool TryResolvePreviewPersona(
        HttpContext context,
        IReadOnlyList<string> roles,
        out TelecomMenuPersona persona)
    {
        persona = default;
        if (!CanPreviewPersona(roles))
        {
            return false;
        }

        if (!context.Request.Headers.TryGetValue(TelecomPreviewHeaders.PreviewPersona, out var values))
        {
            return false;
        }

        var raw = values.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(raw)
            && Enum.TryParse<TelecomMenuPersona>(raw, true, out persona);
    }

    private static bool TryResolvePersonaFromClaims(ClaimsPrincipal user, out TelecomMenuPersona persona)
    {
        persona = default;
        var raw = user.FindFirstValue(TelecomAuthClaims.PrimaryMenuPersona);
        return !string.IsNullOrWhiteSpace(raw)
            && Enum.TryParse<TelecomMenuPersona>(raw, true, out persona);
    }

    private static bool CanPreviewPersona(IReadOnlyList<string> roles) =>
        roles.Any(r =>
            string.Equals(r, "TelecomAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "TelecomManagement", StringComparison.OrdinalIgnoreCase));
}
