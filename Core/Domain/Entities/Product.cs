using Domain.Common;

namespace Domain.Entities;

/// <summary>عنصر كتالوج خدمات BSS (غير مادي افتراضياً للاتصالات).</summary>
public class Product : BaseEntity
{
    public string? Name { get; set; }
    public string? Number { get; set; }
    public string? Description { get; set; }
    public double? UnitPrice { get; set; }
    public bool? Physical { get; set; } = false;

    public string? CompatibleSubscriptionTypeId { get; set; }
    public TelecomSubscriptionTypeLookup? CompatibleSubscriptionTypeLookup { get; set; }
    public string? ServiceCode { get; set; }
}
