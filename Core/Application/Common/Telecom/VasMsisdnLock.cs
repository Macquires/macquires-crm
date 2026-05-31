using System.Collections.Concurrent;

namespace Application.Common.Telecom;

/// <summary>Prevents concurrent VAS toggle / HLR commands for the same MSISDN.</summary>
public sealed class VasMsisdnLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<T> RunExclusiveAsync<T>(string msisdn, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var key = msisdn.Trim();
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            sem.Release();
        }
    }
}
