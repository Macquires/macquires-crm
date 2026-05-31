using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Organizational unit (HQ / region / branch tree).</summary>
public class OrgUnit : BaseEntity
{
    public string? ParentId { get; set; }
    public OrgUnit? Parent { get; set; }
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public OrgUnitKind Kind { get; set; } = OrgUnitKind.Branch;
    /// <summary>Branch / unit manager (AspNetUsers.Id).</summary>
    public string? ManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
