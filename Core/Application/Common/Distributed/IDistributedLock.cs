namespace Application.Common.Distributed;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);
}
