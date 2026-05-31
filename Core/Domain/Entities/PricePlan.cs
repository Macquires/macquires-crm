using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A pricing option for a <see cref="ProductOffering"/>.
/// One offering can have multiple price plans (e.g. monthly 25,000 SYP vs daily 1,200 SYP).
/// </summary>
public class PricePlan : BaseEntity
{
    public string ProductOfferingId { get; set; } = null!;
    public ProductOffering? ProductOffering { get; set; }

    /// <summary>Pricing model: monthly, daily, pay-as-you-go, one-time, etc.</summary>
    public PricePlanType PlanType { get; set; }

    /// <summary>Recurring or one-time price amount.</summary>
    public decimal Price { get; set; }

    public decimal? PricePerMinute { get; set; }
    public decimal? PricePerMegabyte { get; set; }
    public decimal? PricePerSms { get; set; }

    /// <summary>ISO 4217 currency code (defaults to SYP for Syria).</summary>
    public string CurrencyCode { get; set; } = "SYP";

    /// <summary>Optional one-time activation / installation fee (charged once on subscription start).</summary>
    public decimal? ActivationFee { get; set; }

    /// <summary>Validity in days for non-recurring plans (e.g. 30 for monthly, 7 for weekly).</summary>
    public int? ValidityDays { get; set; }

    /// <summary>When true, this is the default price plan presented in the showroom.</summary>
    public bool IsDefault { get; set; }
}
