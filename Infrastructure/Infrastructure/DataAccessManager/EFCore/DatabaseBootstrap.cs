using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataAccessManager.EFCore;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(
        DataContext dataContext,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Database:UseEfMigrations", true))
        {
            throw new InvalidOperationException(
                "Database:UseEfMigrations must be true. EnsureCreated bootstrap was removed.");
        }

        logger.LogInformation("Applying EF Core migrations...");
        await dataContext.Database.MigrateAsync(cancellationToken);
    }
}
