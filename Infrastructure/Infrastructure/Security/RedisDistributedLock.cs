using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Security;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);
}

public sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IDistributedCache _cache;

    public RedisDistributedLock(IDistributedCache cache) => _cache = cache;

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var lockKey = $"lock:{key}";
        var token = Guid.CreateVersion7().ToString();
        await _cache.SetStringAsync(lockKey, token, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, cancellationToken);

        var current = await _cache.GetStringAsync(lockKey, cancellationToken);
        if (!string.Equals(current, token, StringComparison.Ordinal))
        {
            return null;
        }

        return new LockHandle(_cache, lockKey, token);
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly IDistributedCache _cache;
        private readonly string _key;
        private readonly string _token;

        public LockHandle(IDistributedCache cache, string key, string token)
        {
            _cache = cache;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            var current = await _cache.GetStringAsync(_key);
            if (string.Equals(current, _token, StringComparison.Ordinal))
            {
                await _cache.RemoveAsync(_key);
            }
        }
    }
}
