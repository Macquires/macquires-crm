namespace Application.Common.Security;

/// <summary>Backfills <c>NationalIdSearchHash</c> for legacy individuals (seed/import before hash column).</summary>
public interface INationalIdSearchHashBackfillService
{
    Task<int> BackfillAllMissingAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds a 10-digit national ID on individuals missing search hash and self-heals the hash.</summary>
    Task<string?> TryResolveLegacyIndividualIdAsync(string nationalId, CancellationToken cancellationToken = default);
}
