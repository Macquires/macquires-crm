namespace Application.Common.Integrations;

/// <summary>Shared constants for global-settings integration emergency (fallback) mode.</summary>
public static class IntegrationCircuitBreaker
{
    public const string ResponseStatusFallback = "FALLBACK_MODE";
    public const string ResponseStatusSyncReplay = "SYNC_REPLAY";

    public const string FallbackMessageAr =
        "تمت المعالجة تحت وضع الطوارئ (محاكاة) — سيتم مزامنة الشبكة عند إعادة تفعيل التكامل.";

    public const string FallbackNotesEn =
        "Operation processed under structural circuit breaker simulation";
}
