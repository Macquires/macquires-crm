using Application.Common.Services.SecurityManager;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.SecurityManager.AspNetIdentity;

public class ApplicationUser : IdentityUser
{

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? ProfilePictureName { get; set; }
    public bool? IsBlocked { get; set; }
    public bool? IsDeleted { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }

    /// <summary>Explicit UI persona; overrides role-derived persona when set.</summary>
    public TelecomMenuPersona? PrimaryMenuPersona { get; set; }

    public string? ManagerUserId { get; set; }
    public ApplicationUser? Manager { get; set; }

    public string? OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }


    public ApplicationUser(
        string email,
        string firstName,
        string lastName,
        string companyName = "",
        string createdById = ""
        )
    {
        EmailConfirmed = true;
        IsBlocked = false;
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;
        Email = email.Trim();
        UserName = Email;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CompanyName = companyName.Trim();
        CreatedById = createdById.Trim();
    }

}

