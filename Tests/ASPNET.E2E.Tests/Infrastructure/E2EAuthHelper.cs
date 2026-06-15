using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Application.Common.Services.SecurityManager;
using Application.Common.Security;
using Application.Common.Telecom;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASPNET.E2E.Tests.Infrastructure;

public static class E2EAuthHelper
{
    public const string BranchA = "e2e-branch-abu-rammaneh";
    public const string BranchB = "e2e-branch-mazzeh";

    public static async Task<string> LoginAdminAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Security/Login",
            new { email = "admin@root.com", password = "123456" },
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var token = json!.RootElement.GetProperty("content").GetProperty("data").GetProperty("accessToken").GetString();
        return token ?? throw new InvalidOperationException("Admin login did not return access token.");
    }

    public static async Task<string> CreateBranchScopedTokenAsync(
        IServiceProvider services,
        IReadOnlyList<string> roles,
        string? branchId,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "admin@root.com", cancellationToken)
            ?? throw new InvalidOperationException("Default admin user not found.");

        var claims = roles
            .Select(r => new Claim(ClaimTypes.Role, r))
            .ToList();

        if (!string.IsNullOrWhiteSpace(branchId))
        {
            claims.Add(new Claim(TelecomAuthClaims.BranchId, branchId));
        }

        claims.Add(new Claim(TelecomAuthClaims.PrimaryMenuPersona, TelecomMenuPersona.Retail.ToString()));

        return tokenService.GenerateToken(user, claims, expireMinutes: 60);
    }

    public static async Task<string> CreateShowroomBranchTokenAsync(
        IServiceProvider services,
        string branchId,
        CancellationToken cancellationToken = default) =>
        await CreateBranchScopedTokenAsync(
            services,
            [TelecomEnterpriseRoleMatrix.RoleShowroom],
            branchId,
            cancellationToken);
}
