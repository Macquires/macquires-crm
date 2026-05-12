using System.Data;
using Application.Common.CQS.Commands;
using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.DataAccessManager.EFCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Infrastructure.DataAccessManager.EFCore;



public static class DI
{
    public static IServiceCollection RegisterDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var databaseProvider = configuration["DatabaseProvider"];

        // Register Context
        switch (databaseProvider)
        {
            //case "MySql":
            //    services.AddDbContext<DataContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    services.AddDbContext<CommandContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    services.AddDbContext<QueryContext>(options =>
            //        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
            //        .LogTo(Log.Information, LogLevel.Information)
            //        .EnableSensitiveDataLogging()
            //    );
            //    break;

            case "SqlServer":
            default:
                services.AddDbContext<DataContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<CommandContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<QueryContext>(options =>
                    options.UseSqlServer(connectionString)
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                break;
        }


        services.AddScoped<ICommandContext, CommandContext>();
        services.AddScoped<IQueryContext, QueryContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));


        return services;
    }

    public static IHost CreateDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var dataContext = serviceProvider.GetRequiredService<DataContext>();
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DI));

        dataContext.Database.EnsureCreated();

        // EnsureCreated does not add tables to an existing database created before new entities (e.g. telecom).
        if (!RelationalSchemaTableExists(dataContext, "SubscriberProfile"))
        {
            var allowRecreate = configuration.GetValue(
                "Database:AllowDropAndRecreateWhenTelecomTablesMissing",
                environment.IsDevelopment());

            if (allowRecreate)
            {
                logger.LogWarning(
                    "Database is missing telecom tables (SubscriberProfile). Dropping and recreating the database. " +
                    "Set Database:AllowDropAndRecreateWhenTelecomTablesMissing to false to disable this (you must then migrate or restore backup).");
                dataContext.Database.EnsureDeleted();
                dataContext.Database.EnsureCreated();
            }
            else
            {
                throw new InvalidOperationException(
                    "The database exists but is missing required tables (e.g. SubscriberProfile). " +
                    "This happens when the schema was created before telecom features. " +
                    "Fix: drop the database and restart, enable EF migrations, or set Database:AllowDropAndRecreateWhenTelecomTablesMissing=true in Development only.");
            }
        }

        return host;
    }

    /// <summary>SQL Server–compatible existence check (works after EnsureCreated no-op on stale DB).</summary>
    private static bool RelationalSchemaTableExists(DbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
            connection.Open();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = SCHEMA_NAME()
                  AND TABLE_NAME = @tn
                  AND TABLE_TYPE = 'BASE TABLE'
                """;
            var p = cmd.CreateParameter();
            p.ParameterName = "@tn";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            var scalar = cmd.ExecuteScalar();
            return Convert.ToInt32(scalar) > 0;
        }
        finally
        {
            if (!wasOpen)
                connection.Close();
        }
    }
}


