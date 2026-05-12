namespace Infrastructure.TelecomIntegrations;

public sealed class TelecomBillingOptions
{
    public const string SectionName = "TelecomBilling";

    /// <summary>Simulated CBS failures before a successful attempt (demo: Polly retries + log history).</summary>
    public int FailAttemptsBeforeSuccess { get; set; } = 2;

    public int MaxRetryAttempts { get; set; } = 4;

    public int RetryBaseDelayMs { get; set; } = 120;

    public string IntegrationTarget { get; set; } = "HuaweiCBS-Mock";
}
