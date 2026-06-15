using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace ASPNET.E2E.Tests.Infrastructure;

internal static class E2ETestContainers
{
    internal const string SqlPassword = "Your_strong_Password123!";

    internal static MsSqlContainer CreateSqlServer() =>
        new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(SqlPassword)
            .Build();

    internal static RedisContainer CreateRedis() =>
        new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
}
