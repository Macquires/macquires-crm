namespace Application.Common.Integrations;

public interface IIdempotencyStore
{
    Task<bool> TryBeginAsync(string scope, string key, string? requestHash, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task CompleteAsync(string scope, string key, string? responsePayload, CancellationToken cancellationToken = default);
    Task<string?> GetCompletedResponseAsync(string scope, string key, CancellationToken cancellationToken = default);
}
