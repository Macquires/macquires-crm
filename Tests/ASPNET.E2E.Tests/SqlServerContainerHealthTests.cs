using System.Net;
using ASPNET.E2E.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;
using Xunit;

namespace ASPNET.E2E.Tests;

[Trait("Category", "Integration")]
public class SqlServerContainerHealthTests : IAsyncLifetime
{
    private MsSqlContainer? _sql;
    public bool DockerUnavailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (!DockerProbe.IsAvailable())
        {
            DockerUnavailable = true;
            return;
        }

        _sql = E2ETestContainers.CreateSqlServer();
        await _sql.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task SqlServer_testcontainer_is_reachable()
    {
        Skip.If(DockerUnavailable, "Docker daemon not running.");
        Assert.False(string.IsNullOrWhiteSpace(_sql!.GetConnectionString()));
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_sql.GetConnectionString());
        await connection.OpenAsync();
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }
}

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_success_or_degraded()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status: {response.StatusCode}");
    }
}
