using Domain.Common;

namespace Domain.Entities;

/// <summary>Per-attempt log for external billing/charging (demo resilience story).</summary>
public class BillingIntegrationLog : BaseEntity
{
    public string? TelecomOperationRequestId { get; set; }
    public TelecomOperationRequest? TelecomOperationRequest { get; set; }

    public string? TelecomPaymentTransactionId { get; set; }
    public TelecomPaymentTransaction? TelecomPaymentTransaction { get; set; }

    public int AttemptNumber { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string? IntegrationTarget { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
}
