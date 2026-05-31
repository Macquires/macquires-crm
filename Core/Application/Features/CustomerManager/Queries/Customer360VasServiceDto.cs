namespace Application.Features.CustomerManager.Queries;

public record Customer360VasServiceDto(
    string Id,
    string TelecomSubscriptionId,
    string Msisdn,
    string ServiceNameAr,
    string? ServiceCode,
    string Status,
    DateTime? ActivatedAtUtc,
    DateTime? DeactivatedAtUtc);
