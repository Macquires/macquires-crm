using System.Collections.Concurrent;
using Application.Common.Distributed;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Infrastructure.Security;

public sealed class RedisDistributedLock : IDistributedLock
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> LocalLocks = new(StringComparer.Ordinal);
    private readonly IConnectionMultiplexer? _redis;

    public RedisDistributedLock(IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connection))
        {
            try
            {
                _redis = ConnectionMultiplexer.Connect(connection);
            }
            catch
            {
                _redis = null;
            }
        }
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var lockKey = $"lock:{key}";
            var token = Guid.NewGuid().ToString("N");
            var acquired = await db.StringSetAsync(lockKey, token, ttl, When.NotExists);
            if (!acquired)
            {
                return null;
            }

            return new RedisLockHandle(db, lockKey, token);
        }

        var semaphore = LocalLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        if (!await semaphore.WaitAsync(0, cancellationToken))
        {
            return null;
        }

        return new SemaphoreLockHandle(semaphore);
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public RedisLockHandle(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            var current = await _db.StringGetAsync(_key);
            if (current == _token)
            {
                await _db.KeyDeleteAsync(_key);
            }
        }
    }

    private sealed class SemaphoreLockHandle : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public SemaphoreLockHandle(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public ValueTask DisposeAsync()
        {
            _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
