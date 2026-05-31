using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// TM Forum SID — Product Offering: the commercially visible package sold to a subscriber.
/// Example: "سيريتل ميكس 500" = 500 min voice + 20 GB data + 100 SMS at 25,000 SYP/month.
/// One offering has many <see cref="ProductOfferingComponent"/> and many <see cref="PricePlan"/>.
/// </summary>
public class ProductOffering : BaseEntity
{
    /// <summary>Arabic display name shown in showroom / self-care.</summary>
    public string Name { get; set; } = null!;

    /// <summary>English display name (optional — bilingual UX).</summary>
    public string? NameEn { get; set; }

    public string? Description { get; set; }

    /// <summary>Stable machine-readable code for CBS / provisioning (e.g. "MIX_500").</summary>
    public string Code { get; set; } = null!;

    /// <summary>When set, the offering is only compatible with lines of this subscription type.</summary>
    public string? CompatibleSubscriptionTypeId { get; set; }
    public TelecomSubscriptionTypeLookup? CompatibleSubscriptionType { get; set; }

    /// <summary>
    /// Optional link to the technical <see cref="Product"/> (CBS / Huawei service row) used for subscriptions and provisioning.
    /// Wizard and migration flows resolve billing from this product while displaying the commercial offering.
    /// </summary>
    public string? ProductId { get; set; }
    public Product? Product { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Commercial availability window — null means no restriction.</summary>
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }

    /// <summary>Sort order for catalog display.</summary>
    public int SortOrder { get; set; }

    /// <summary>Components that make up this offering (voice, data, SMS, VAS, equipment).</summary>
    public ICollection<ProductOfferingComponent> Components { get; set; } = new List<ProductOfferingComponent>();

    /// <summary>Pricing options for this offering.</summary>
    public ICollection<PricePlan> PricePlans { get; set; } = new List<PricePlan>();

    // ─── Smart Product Catalog Architect Properties ───

    public PaymentType? PaymentType { get; set; }
    public BillingCycle? BillingCycleEnum { get; set; }

    /// <summary>Eligibility rules: Prepaid, Postpaid, or Hybrid (legacy string — prefer <see cref="PaymentType"/>).</summary>
    public string? EligibilityRules { get; set; }

    /// <summary>Asset compatibility: Voice, Data, or Fiber.</summary>
    public string? AssetCompatibility { get; set; }

    /// <summary>Billing cycle: Monthly, Weekly, Daily, OneTime.</summary>
    public string? BillingCycle { get; set; }

    /// <summary>Tax category: StandardVAT, TelecomTax, Exempt.</summary>
    public string? TaxCategory { get; set; }

    /// <summary>Service ID / SOC Code for Huawei CBS integration.</summary>
    public string? ServiceIdSocCode { get; set; }

    /// <summary>Speed / Data quota limit in GB.</summary>
    public double? SpeedQuotaLimitGb { get; set; }

    /// <summary>Voice minutes limit.</summary>
    public int? VoiceMinutesLimit { get; set; }

    /// <summary>Throttling policy: Cutoff, Throttle128K, PayAsYouGo.</summary>
    public string? ThrottlingPolicy { get; set; }

    /// <summary>Frontend rendering icon class (e.g. bi-phone, bi-wifi).</summary>
    public string? IconClass { get; set; }

    /// <summary>Frontend rendering badge color (e.g. primary, gold, success).</summary>
    public string? BadgeColor { get; set; }

    /// <summary>Marketing short description shown in wizards.</summary>
    public string? ShortDescription { get; set; }
}
