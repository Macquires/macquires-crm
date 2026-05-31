namespace Application.Common.Services.SecurityManager;

public record LoginResultDto
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? Avatar { get; init; }
    public List<MenuNavigationTreeNodeDto>? MenuNavigation { get; init; }
    public List<string>? Roles { get; init; }
    /// <summary>Resolved operator persona for sidebar/dashboard (e.g. CallCenter, Executive).</summary>
    public string? PrimaryMenuPersona { get; init; }

    /// <summary>Server-resolved landing route after successful login.</summary>
    public string? LandingPath { get; init; }

    /// <summary>Effective permission keys for client-side nav and landing fallback.</summary>
    public List<string>? Permissions { get; init; }
}
