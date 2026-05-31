using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Telecom support ticket escalated from call center to back office (MSISDN / HLR / provisioning).</summary>
public class TelecomTechnicalTicket : BaseEntity
{
    public string TicketNumber { get; set; } = null!;
    public string Msisdn { get; set; } = null!;
    public string? CustomerId { get; set; }
    public string? SubscriberProfileId { get; set; }
    public TechnicalTicketIssueType IssueType { get; set; }
    public TechnicalTicketCategory TicketCategory { get; set; } = TechnicalTicketCategory.Complaint;
    public TechnicalTicketPriority Priority { get; set; }
    public TechnicalTicketStatus Status { get; set; } = TechnicalTicketStatus.Open;
    public string? AssignedToGroupId { get; set; }
    public string OpenedByUserId { get; set; } = null!;
    public string? ResolvedByUserId { get; set; }
    public string? Notes { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    /// <summary>Creation channel: CallCenter_Agent, Customer_Care_Voice_AI, Showroom_Agent, Self_Care_App.</summary>
    public string CreatedByChannel { get; set; } = "CallCenter_Agent";
}
