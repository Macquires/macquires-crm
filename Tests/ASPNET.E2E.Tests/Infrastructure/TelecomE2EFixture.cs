extern alias Simulator;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace ASPNET.E2E.Tests.Infrastructure;

public class TelecomE2EFixture : IAsyncLifetime
{
    private MsSqlContainer? _sql;
    private RedisContainer? _redis;

    public bool DockerUnavailable { get; private set; }

    public WebApplicationFactory<Simulator::Program> SimulatorFactory { get; private set; } = null!;
    public NsuiteWebApplicationFactory AppFactory { get; private set; } = null!;
    public string PublicBaseUrl => AppFactory.PublicBaseUrl;
    public HttpClient SimulatorClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!DockerProbe.IsAvailable())
        {
            DockerUnavailable = true;
            SimulatorFactory = new WebApplicationFactory<Simulator::Program>();
            SimulatorClient = new HttpClient();
            AppFactory = new NsuiteWebApplicationFactory("Server=localhost;Database=skip;", "http://localhost:5099");
            return;
        }

        _sql = E2ETestContainers.CreateSqlServer();
        _redis = E2ETestContainers.CreateRedis();
        await Task.WhenAll(_sql.StartAsync(), _redis.StartAsync());

        SimulatorFactory = new WebApplicationFactory<Simulator::Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));
        SimulatorClient = SimulatorFactory.CreateClient();

        var simulatorBase = SimulatorClient.BaseAddress?.ToString().TrimEnd('/')
            ?? throw new InvalidOperationException("Simulator base address missing.");

        AppFactory = new NsuiteWebApplicationFactory(
            _sql.GetConnectionString(),
            simulatorBase,
            _redis.GetConnectionString());

        using var warmup = AppFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });
        warmup.Timeout = TimeSpan.FromMinutes(10);
        _ = await warmup.GetAsync("/health");
    }

    public async Task DisposeAsync()
    {
        SimulatorClient?.Dispose();
        SimulatorFactory?.Dispose();
        AppFactory?.Dispose();
        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }

        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }
}
