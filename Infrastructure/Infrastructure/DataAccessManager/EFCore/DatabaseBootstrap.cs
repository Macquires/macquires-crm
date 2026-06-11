using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataAccessManager.EFCore;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(
        DataContext dataContext,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        Action<DataContext, ILogger> applyLegacyPatches,
        CancellationToken cancellationToken = default)
    {
        var useMigrations = configuration.GetValue("Database:UseEfMigrations", false);

        if (useMigrations)
        {
            logger.LogInformation("Applying EF Core migrations...");
            await dataContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        dataContext.Database.EnsureCreated();

        if (!RelationalSchemaTableExists(dataContext, "SubscriberProfile"))
        {
            var allowRecreate = configuration.GetValue(
                "Database:AllowDropAndRecreateWhenTelecomTablesMissing",
                environment.IsDevelopment());

            if (allowRecreate)
            {
                logger.LogWarning(
                    "Database is missing telecom tables (SubscriberProfile). Dropping and recreating the database.");
                dataContext.Database.EnsureDeleted();
                dataContext.Database.EnsureCreated();
            }
            else
            {
                throw new InvalidOperationException(
                    "Database exists but is missing required telecom tables. Enable Database:UseEfMigrations or recreate.");
            }
        }

        applyLegacyPatches(dataContext, logger);
    }

    private static bool RelationalSchemaTableExists(DataContext dataContext, string tableName)
    {
        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = @table) THEN 1 ELSE 0 END
                """;
            var p = cmd.CreateParameter();
            p.ParameterName = "@table";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }
}
