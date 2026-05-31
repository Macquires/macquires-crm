using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One component (service specification) inside a <see cref="ProductOffering"/>.
/// Examples: "500 دقيقة محلية" (Voice, 500 min), "20 جيجا بيانات" (Data, 20 GB), "100 رسالة" (SMS, 100).
/// </summary>
public class ProductOfferingComponent : BaseEntity
{
    public string ProductOfferingId { get; set; } = null!;
    public ProductOffering? ProductOffering { get; set; }

    /// <summary>The type of service this component represents.</summary>
    public ServiceComponentType ComponentType { get; set; }

    /// <summary>Human-readable label, e.g. "500 دقيقة محلية" or "20 GB Data".</summary>
    public string? Label { get; set; }

    /// <summary>Numeric quota value: 500 (minutes), 20 (GB), 100 (SMS). Null when unlimited.</summary>
    public decimal? Quota { get; set; }

    /// <summary>Unit of the quota: "minutes", "GB", "MB", "SMS", "days".</summary>
    public string? QuotaUnit { get; set; }

    /// <summary>When true, quota is ignored and the component grants unlimited usage.</summary>
    public bool IsUnlimited { get; set; }

    /// <summary>Display sort order within the offering.</summary>
    public int SortOrder { get; set; }

    /// <summary>When set, this VAS/component requires another offering to be active first.</summary>
    public string? RequiresProductOfferingId { get; set; }
    public ProductOffering? RequiresProductOffering { get; set; }
}
