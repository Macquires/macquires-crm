using Application.Common.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Distributed;

public static class HostedServiceLockExtensions
{
    public static async Task<bool> TryRunUnderDistributedLockAsync(
        this IServiceScope scope,
        string lockKey,
        TimeSpan lockTtl,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();
        await using var handle = await distributedLock.TryAcquireAsync(lockKey, lockTtl, cancellationToken);
        if (handle is null)
        {
            return false;
        }

        await action(cancellationToken);
        return true;
    }
}
