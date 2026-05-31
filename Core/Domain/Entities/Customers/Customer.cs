using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// طرف العميل في BSS (TPH). الهوية القانونية هنا؛ الخطوط على <see cref="SubscriberProfile"/>.
/// </summary>
public abstract class Customer : BaseEntity
{
    public CustomerKind CustomerKind { get; protected set; }
    public string DisplayName { get; protected set; } = null!;
    public string AccountNumber { get; protected set; } = null!;
    public string? Description { get; protected set; }
    public CustomerStatus Status { get; protected set; } = CustomerStatus.Active;
    public CustomerStatusReasonCode StatusReasonCode { get; protected set; } = CustomerStatusReasonCode.None;
    public string? StatusReasonNote { get; protected set; }
    public PostalAddress Address { get; protected set; } = PostalAddress.Empty;
    public string? ContactEmail { get; protected set; }
    public string? PrimaryPhone { get; protected set; }
    public string? FaxNumber { get; protected set; }
    public string? Website { get; protected set; }
    public string? WhatsApp { get; protected set; }
    public string? LinkedIn { get; protected set; }
    public string? Facebook { get; protected set; }
    public string? Instagram { get; protected set; }
    public string? TwitterX { get; protected set; }
    public string? TikTok { get; protected set; }
    public string? CustomerGroupId { get; protected set; }
    public CustomerGroup? CustomerGroup { get; protected set; }
    public string? CustomerCategoryId { get; protected set; }
    public CustomerCategory? CustomerCategory { get; protected set; }

    /// <summary>Home branch for strategic MIS scoping.</summary>
    public string? OrgUnitId { get; protected set; }
    public OrgUnit? OrgUnit { get; protected set; }

    public ICollection<CustomerContact> CustomerContactList { get; protected set; } = new List<CustomerContact>();
    public ICollection<CustomerIdentityDocument> IdentityDocuments { get; protected set; } = new List<CustomerIdentityDocument>();
    public ICollection<SubscriberProfile> SubscriberProfiles { get; protected set; } = new List<SubscriberProfile>();

    protected Customer() { }

    public void UpdateContact(string? email, string? primaryPhone, string? fax, string? website)
    {
        ContactEmail = email;
        PrimaryPhone = primaryPhone;
        FaxNumber = fax;
        Website = website;
    }

    public void UpdateAddress(PostalAddress address) => Address = address;

    public void UpdateSocial(string? whatsApp, string? linkedIn, string? facebook, string? instagram, string? twitterX, string? tikTok)
    {
        WhatsApp = whatsApp;
        LinkedIn = linkedIn;
        Facebook = facebook;
        Instagram = instagram;
        TwitterX = twitterX;
        TikTok = tikTok;
    }

    public void Suspend() => Status = CustomerStatus.Suspended;

    public void Activate() => Status = CustomerStatus.Active;

    public void Close() => Status = CustomerStatus.Closed;

    public void Blacklist(CustomerStatusReasonCode reason, string? note = null)
    {
        Status = CustomerStatus.Blacklisted;
        StatusReasonCode = reason;
        StatusReasonNote = note;
    }

    public void ClearBlacklist()
    {
        if (Status != CustomerStatus.Blacklisted) return;
        Status = CustomerStatus.Active;
        StatusReasonCode = CustomerStatusReasonCode.None;
        StatusReasonNote = null;
    }

    public void SetDisplayName(string displayName) => DisplayName = displayName;
    public void SetDescription(string? description) => Description = description;
    public void SetCustomerGroup(string? groupId, string? categoryId)
    {
        CustomerGroupId = groupId;
        CustomerCategoryId = categoryId;
    }

    public void SetOrgUnitId(string? orgUnitId) => OrgUnitId = orgUnitId;
}
