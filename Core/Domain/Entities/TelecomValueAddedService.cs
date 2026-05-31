using Domain.Common;

namespace Domain.Entities;

/// <summary>Standalone VAS catalog (eClip, roaming, SyriaTel Cash) — not a product offering / package.</summary>
public class TelecomValueAddedService : BaseEntity
{
    public string ServiceCode { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public decimal MonthlyFee { get; set; }
    public bool IsActive { get; set; } = true;
    public string HlrCommandTemplate { get; set; } = null!;
    public int SortOrder { get; set; }

    public ICollection<SubscriberActiveService> ActiveSubscriptions { get; set; } = new List<SubscriberActiveService>();
}
