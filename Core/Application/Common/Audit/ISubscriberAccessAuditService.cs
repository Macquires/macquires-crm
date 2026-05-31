namespace Application.Common.Audit;

/// <summary>Audit trail when staff search or open subscriber profiles (call center compliance).</summary>
public interface ISubscriberAccessAuditService
{
    Task LogSearchAsync(
        string searchChannel,
        string? nationalId,
        string? phoneOrMsisdn,
        string? freeTextTerm,
        IReadOnlyList<SubscriberSearchMatchAuditDto> matches,
        CancellationToken cancellationToken = default);

    Task LogProfileViewAsync(
        string customerId,
        string? displayName,
        string? primaryPhoneOrMsisdn,
        CancellationToken cancellationToken = default);
}

public sealed class SubscriberSearchMatchAuditDto
{
    public string CustomerId { get; init; } = "";
    public string? Name { get; init; }
    public string? PhoneOrMsisdn { get; init; }
}
