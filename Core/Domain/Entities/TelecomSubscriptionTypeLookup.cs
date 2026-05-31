using Domain.Common;

namespace Domain.Entities;

/// <summary>User-managed telecom line / subscription type (display names + stable integration code).</summary>
public class TelecomSubscriptionTypeLookup : BaseEntity
{
    /// <summary>Stable code for integrations (e.g. PREPAID). Not the same as display labels.</summary>
    public string Code { get; set; } = null!;

    public string NameAr { get; set; } = null!;
    public string NameEn { get; set; } = null!;

    /// <summary>Optional UI color (e.g. #c8102e).</summary>
    public string? DisplayColor { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Exactly one active row should be default for new lines when type is omitted.</summary>
    public bool IsDefault { get; set; }
}
