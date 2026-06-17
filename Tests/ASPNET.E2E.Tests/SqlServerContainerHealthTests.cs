extern alias Simulator;

using System.Net;
using ASPNET.E2E.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
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

[Trait("Category", "Integration")]
public class HealthEndpointTests : IAsyncLifetime
{
    private MsSqlContainer? _sql;
    private WebApplicationFactory<Simulator::Program>? _simulatorFactory;
    private NsuiteWebApplicationFactory? _factory;
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

        _simulatorFactory = new WebApplicationFactory<Simulator::Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));

        using var simulatorClient = _simulatorFactory.CreateClient();
        var simulatorBase = simulatorClient.BaseAddress?.ToString().TrimEnd('/')
            ?? throw new InvalidOperationException("Simulator base address missing.");

        _factory = new NsuiteWebApplicationFactory(_sql.GetConnectionString(), simulatorBase);

        using var warmup = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });
        warmup.Timeout = TimeSpan.FromMinutes(10);
        _ = await warmup.GetAsync("/health");
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        _simulatorFactory?.Dispose();
        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Health_endpoint_returns_success_or_degraded()
    {
        Skip.If(DockerUnavailable, "Docker daemon not running.");

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });
        client.Timeout = TimeSpan.FromMinutes(5);

        var response = await client.GetAsync("/health");
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status: {response.StatusCode}");
    }
}
