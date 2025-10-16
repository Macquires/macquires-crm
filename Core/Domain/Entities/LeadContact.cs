using Domain.Common;

namespace Domain.Entities;

public class LeadContact : BaseEntity
{
    public string? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string? Number { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZipCode { get; set; }
    public string? AddressCountry { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? WhatsApp { get; set; }
    public string? LinkedIn { get; set; }
    public string? Facebook { get; set; }
    public string? Twitter { get; set; }
    public string? Instagram { get; set; }
    public string? AvatarName { get; set; }
}
