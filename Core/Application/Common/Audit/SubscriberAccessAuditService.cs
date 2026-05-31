using Application.Common.Security;
using Application.Common.Telecom;

namespace Application.Common.Audit;

public sealed class SubscriberAccessAuditService : ISubscriberAccessAuditService
{
    private readonly IUserAuditService _audit;
    private readonly IOperatorContext _operator;

    public SubscriberAccessAuditService(IUserAuditService audit, IOperatorContext operatorContext)
    {
        _audit = audit;
        _operator = operatorContext;
    }

    public async Task LogSearchAsync(
        string searchChannel,
        string? nationalId,
        string? phoneOrMsisdn,
        string? freeTextTerm,
        IReadOnlyList<SubscriberSearchMatchAuditDto> matches,
        CancellationToken cancellationToken = default)
    {
        if (!_operator.IsAuthenticated || string.IsNullOrWhiteSpace(_operator.UserId))
        {
            return;
        }

        var criteria = BuildCriteriaLabel(nationalId, phoneOrMsisdn, freeTextTerm);
        if (string.IsNullOrEmpty(criteria))
        {
            return;
        }

        var count = matches?.Count ?? 0;
        var single = count == 1 ? matches![0] : null;
        var summary = count switch
        {
            0 => $"بحث مشترك ({searchChannel}): {criteria} — لا نتائج",
            1 => $"بحث مشترك ({searchChannel}): {criteria} → {single!.Name ?? single.PhoneOrMsisdn ?? single.CustomerId}",
            _ => $"بحث مشترك ({searchChannel}): {criteria} — {count} نتائج",
        };

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = _operator.UserId!,
                ActionType = UserAuditActionTypes.SubscriberSearched,
                EntityType = nameof(Domain.Entities.Customer),
                EntityId = single?.CustomerId,
                SummaryAr = summary,
                Payload = AuditLogPayloadFactory.SubscriberSearch(
                    searchChannel,
                    criteria,
                    matches ?? Array.Empty<SubscriberSearchMatchAuditDto>()),
            },
            cancellationToken);
    }

    public async Task LogProfileViewAsync(
        string customerId,
        string? displayName,
        string? primaryPhoneOrMsisdn,
        CancellationToken cancellationToken = default)
    {
        if (!_operator.IsAuthenticated || string.IsNullOrWhiteSpace(_operator.UserId))
        {
            return;
        }

        var label = displayName ?? customerId;
        var phonePart = string.IsNullOrWhiteSpace(primaryPhoneOrMsisdn) ? "" : $" · {primaryPhoneOrMsisdn}";

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = _operator.UserId!,
                ActionType = UserAuditActionTypes.CustomerViewed,
                EntityType = nameof(Domain.Entities.Customer),
                EntityId = customerId,
                SummaryAr = $"اطلاع على ملف المشترك: {label}{phonePart}",
                Payload = AuditLogPayloadFactory.ProfileView(customerId, displayName, primaryPhoneOrMsisdn),
            },
            cancellationToken);
    }

    private static string BuildCriteriaLabel(string? nationalId, string? phoneOrMsisdn, string? freeTextTerm)
    {
        var parts = new List<string>();
        var nat = (nationalId ?? string.Empty).Trim();
        if (nat.Length >= 2)
        {
            parts.Add($"هوية:{MaskNationalId(nat)}");
        }

        var phone = (phoneOrMsisdn ?? string.Empty).Trim();
        if (phone.Length >= 2)
        {
            var canon = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(phone) ?? phone;
            parts.Add($"خط:{canon}");
        }

        var text = (freeTextTerm ?? string.Empty).Trim();
        if (text.Length >= 2 && parts.Count == 0)
        {
            parts.Add($"نص:{text}");
        }

        return string.Join(" ", parts);
    }

    private static string MaskNationalId(string raw)
    {
        if (raw.Length <= 4)
        {
            return "****";
        }

        return new string('*', raw.Length - 4) + raw[^4..];
    }
}
