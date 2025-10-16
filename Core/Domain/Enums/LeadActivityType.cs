using System.ComponentModel;

namespace Domain.Enums;
public enum LeadActivityType
{
    [Description("Other")]
    Other = 0,
    [Description("Phone")]
    Phone = 1,
    [Description("Email")]
    Email = 2,
    [Description("Social Media")]
    SocialMedia = 3,
    [Description("Meeting")]
    Meeting = 4,
    [Description("Event")]
    Event = 5
}
