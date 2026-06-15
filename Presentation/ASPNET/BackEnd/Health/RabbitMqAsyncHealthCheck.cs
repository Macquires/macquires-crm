using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace ASPNET.BackEnd.Health;

public sealed class RabbitMqAsyncHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed.", ex);
        }
    }
}
