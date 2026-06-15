namespace Application.Features.TelecomManager.Queries;

/// <summary>Strict unified lookup hit — National ID, commercial registry, or MSISDN only.</summary>
public record TelecomUniversalSearchRowDto
{
    public string ResultType { get; init; } = null!;
    public string Id { get; init; } = null!;
    public string? SubscriberProfileId { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerNameAr { get; init; }
    public string? NationalId { get; init; }
    public string? Msisdn { get; init; }
    public string? Status { get; init; }
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
}

public class GetTelecomUniversalSearchResult
{
    public List<TelecomUniversalSearchRowDto> Data { get; init; } = new();
}
