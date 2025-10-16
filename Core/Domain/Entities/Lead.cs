using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Lead : BaseEntity
{
    public string? Number { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDescription { get; set; }
    public string? CompanyAddressStreet { get; set; }
    public string? CompanyAddressCity { get; set; }
    public string? CompanyAddressState { get; set; }
    public string? CompanyAddressZipCode { get; set; }
    public string? CompanyAddressCountry { get; set; }
    public string? CompanyPhoneNumber { get; set; }
    public string? CompanyFaxNumber { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? CompanyWhatsApp { get; set; }
    public string? CompanyLinkedIn { get; set; }
    public string? CompanyFacebook { get; set; }
    public string? CompanyInstagram { get; set; }
    public string? CompanyTwitter { get; set; }
    public DateTime? DateProspecting { get; set; }
    public DateTime? DateClosingEstimation { get; set; }
    public DateTime? DateClosingActual { get; set; }
    public double? AmountTargeted { get; set; }
    public double? AmountClosed { get; set; }
    public double? BudgetScore { get; set; }
    public double? AuthorityScore { get; set; }
    public double? NeedScore { get; set; }
    public double? TimelineScore { get; set; }
    public PipelineStage? PipelineStage { get; set; }
    public ClosingStatus? ClosingStatus { get; set; }
    public string? ClosingNote { get; set; }
    public string? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public string? SalesTeamId { get; set; }
    public SalesTeam? SalesTeam { get; set; }
    public ICollection<LeadContact> LeadContacts { get; set; } = new List<LeadContact>();
    public ICollection<LeadActivity> LeadActivities { get; set; } = new List<LeadActivity>();

}
