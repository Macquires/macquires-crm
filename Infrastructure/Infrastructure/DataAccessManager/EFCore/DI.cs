using System.Data;
using Application.Common.CQS.Commands;
using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Common;
using Domain.Entities;
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
                    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(2),
                        errorNumbersToAdd: null))
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<CommandContext>(options =>
                    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(2),
                        errorNumbersToAdd: null))
                    .LogTo(Log.Information, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );
                services.AddDbContext<QueryContext>(options =>
                    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(2),
                        errorNumbersToAdd: null))
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

        ApplyTelecomOperationRequestSchemaPatches(dataContext, logger);
        ApplyTelecomOperationIdentityDocumentSchemaPatch(dataContext, logger);
        ApplyTelecomBssPrimaryLineSchemaPatches(dataContext, logger);
        ApplyTelecomSubscriptionTypeSchemaPatches(dataContext, logger);
        ApplyProductCompatibleSubscriptionTypeSchemaPatches(dataContext, logger);
        ApplyProductOfferingSmartFieldsPatch(dataContext, logger);
        ApplyProductOfferingProductIdAndOperationOfferingPatch(dataContext, logger);
        ApplySubscriberProfileCustomerIndexNonUniquePatch(dataContext, logger);
        ApplyDomainGateSubscriberProfileLegacyCleanupPatch(dataContext, logger);
        ApplyDashboardWidgetsSchemaPatch(dataContext, logger);
        ApplyAdministrationFoundationSchemaPatches(dataContext, logger);
        ApplyRolePermissionSchemaPatch(dataContext, logger);
        ApplyTelecomTechnicalTicketSchemaPatch(dataContext, logger);
        ApplyTelecomTechnicalTicketCreatedByChannelPatch(dataContext, logger);
        ApplyTelecomTechnicalTicketCategoryPatch(dataContext, logger);
        ApplyMsisdnAssetPairedKitSchemaPatch(dataContext, logger);
        ApplyTelecomIntegrationLogSchemaPatch(dataContext, logger);
        ApplyBulkImportEnterpriseSchemaPatch(dataContext, logger);
        ApplyVasCatalogSchemaPatch(dataContext, logger);
        ApplyNullableBitColumnsDataPatch(dataContext, logger);

        return host;
    }

    /// <summary>
    /// Coalesce NULL <c>bit</c> values (manual migrations / legacy rows) so reads do not throw
    /// <see cref="System.Data.SqlTypes.SqlNullValueException"/>.
    /// </summary>
    private static void ApplyNullableBitColumnsDataPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                DECLARE @sql nvarchar(max) = N'';
                SELECT @sql = @sql + N'
                UPDATE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
                    + N' SET ' + QUOTENAME(c.name) + N' = '
                    + CASE WHEN c.name = N'IsActive' THEN N'1' ELSE N'0' END
                    + N' WHERE ' + QUOTENAME(c.name) + N' IS NULL;'
                FROM sys.columns c
                INNER JOIN sys.tables t ON c.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE c.system_type_id = 104
                  AND c.is_nullable = 1
                  AND t.is_ms_shipped = 0
                  AND t.type = N'U';
                IF LEN(@sql) > 0
                    EXEC sp_executesql @sql;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'IsLostOrStolenReport') IS NOT NULL
                   AND EXISTS (
                       SELECT 1 FROM sys.columns
                       WHERE object_id = OBJECT_ID(N'dbo.TelecomOperationRequest')
                         AND name = N'IsLostOrStolenReport' AND is_nullable = 1)
                BEGIN
                    UPDATE dbo.TelecomOperationRequest SET IsLostOrStolenReport = 0 WHERE IsLostOrStolenReport IS NULL;
                    ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN IsLostOrStolenReport bit NOT NULL;
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Nullable bit column data patch applied (NULL coalesce).");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nullable bit column data patch skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    /// Domain Gate: remove legacy identity columns from <c>SubscriberProfile</c> (identity lives on <c>Customer</c> TPH only).
    /// Idempotent — safe on fresh DBs that never had ERP-era columns.
    /// </summary>
    private static void ApplyDomainGateSubscriberProfileLegacyCleanupPatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (!RelationalSchemaTableExists(dataContext, "SubscriberProfile"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'dbo.SubscriberProfile', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM sys.indexes i
                        WHERE i.object_id = OBJECT_ID(N'dbo.SubscriberProfile')
                          AND i.name = N'IX_SubscriberProfile_CommercialRegistration')
                        DROP INDEX IX_SubscriberProfile_CommercialRegistration ON dbo.SubscriberProfile;

                    IF EXISTS (
                        SELECT 1 FROM sys.indexes i
                        WHERE i.object_id = OBJECT_ID(N'dbo.SubscriberProfile')
                          AND i.name = N'IX_SubscriberProfile_NationalId')
                        DROP INDEX IX_SubscriberProfile_NationalId ON dbo.SubscriberProfile;

                    DECLARE @dropCol NVARCHAR(128);
                    DECLARE @cols TABLE (ColName NVARCHAR(128));
                    INSERT INTO @cols (ColName) VALUES
                        (N'NationalId'),
                        (N'SubscriberType'),
                        (N'DateOfBirth'),
                        (N'CommercialRegistration'),
                        (N'TaxNumber'),
                        (N'AuthorizedSignatory');

                    DECLARE col_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT ColName FROM @cols;
                    OPEN col_cursor;
                    FETCH NEXT FROM col_cursor INTO @dropCol;
                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        IF COL_LENGTH(N'dbo.SubscriberProfile', @dropCol) IS NOT NULL
                        BEGIN
                            DECLARE @dc NVARCHAR(256);
                            SELECT @dc = dc.name
                            FROM sys.default_constraints dc
                            INNER JOIN sys.columns c
                                ON dc.parent_column_id = c.column_id AND dc.parent_object_id = c.object_id
                            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SubscriberProfile')
                              AND c.name = @dropCol;
                            IF @dc IS NOT NULL
                                EXEC(N'ALTER TABLE dbo.SubscriberProfile DROP CONSTRAINT [' + @dc + N']');

                            EXEC(N'ALTER TABLE dbo.SubscriberProfile DROP COLUMN [' + @dropCol + N']');
                        END
                        FETCH NEXT FROM col_cursor INTO @dropCol;
                    END
                    CLOSE col_cursor;
                    DEALLOCATE col_cursor;
                END
                """;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Domain Gate: SubscriberProfile legacy identity column cleanup skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    /// EnsureCreated does not ALTER existing tables. Add columns introduced after first DB creation (Syriatel migration field).
    /// </summary>
    private static void ApplyTelecomOperationRequestSchemaPatches(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var entityType = dataContext.Model.FindEntityType(typeof(TelecomOperationRequest));
        if (entityType == null)
        {
            return;
        }

        var tableName = entityType.GetTableName();
        if (string.IsNullOrEmpty(tableName))
        {
            return;
        }

        var schema = entityType.GetSchema();
        var schemaName = string.IsNullOrEmpty(schema) ? "dbo" : schema;

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = N'TargetOfferName'
                    """;
                var ps = cmd.CreateParameter();
                ps.ParameterName = "@schema";
                ps.Value = schemaName;
                cmd.Parameters.Add(ps);
                var pt = cmd.CreateParameter();
                pt.ParameterName = "@table";
                pt.Value = tableName;
                cmd.Parameters.Add(pt);
                var exists = Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
                if (exists)
                {
                    return;
                }
            }

            logger.LogInformation("Applying schema patch: adding column TargetOfferName to {Schema}.{Table}.", schemaName, tableName);

            using var alter = connection.CreateCommand();
            alter.CommandText = $"""
                ALTER TABLE [{schemaName}].[{tableName}] ADD [TargetOfferName] NVARCHAR(255) NULL;
                """;
            alter.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch for TargetOfferName skipped or failed on {Schema}.{Table}.", schemaName, tableName);
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyTelecomOperationIdentityDocumentSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var entityType = dataContext.Model.FindEntityType(typeof(TelecomOperationRequest));
        if (entityType == null)
        {
            return;
        }

        var tableName = entityType.GetTableName();
        if (string.IsNullOrEmpty(tableName))
        {
            return;
        }

        var schema = entityType.GetSchema();
        var schemaName = string.IsNullOrEmpty(schema) ? "dbo" : schema;
        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = N'IdentityDocumentStorageKey'
                    """;
                var ps = cmd.CreateParameter();
                ps.ParameterName = "@schema";
                ps.Value = schemaName;
                cmd.Parameters.Add(ps);
                var pt = cmd.CreateParameter();
                pt.ParameterName = "@table";
                pt.Value = tableName;
                cmd.Parameters.Add(pt);
                var exists = Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
                if (exists)
                {
                    return;
                }
            }

            logger.LogInformation(
                "Applying schema patch: adding column IdentityDocumentStorageKey to {Schema}.{Table}.",
                schemaName,
                tableName);

            using var alter = connection.CreateCommand();
            alter.CommandText = $"""
                ALTER TABLE [{schemaName}].[{tableName}] ADD [IdentityDocumentStorageKey] NVARCHAR(500) NULL;
                """;
            alter.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Schema patch for IdentityDocumentStorageKey skipped or failed on {Schema}.{Table}.",
                schemaName,
                tableName);
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    /// EnsureCreated does not add new tables/columns on existing DBs. Adds audit table and soft-delete–aware unique MSISDN index (recycling).
    /// Canonical manual script: <c>Migrations/TelecomBssPrimaryLine_Manual.sql</c>.
    /// </summary>
    private static void ApplyTelecomBssPrimaryLineSchemaPatches(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = """
                    IF OBJECT_ID(N'dbo.TelecomMsisdnChangeLog', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.TelecomMsisdnChangeLog (
                            [Id] NVARCHAR(50) NOT NULL CONSTRAINT PK_TelecomMsisdnChangeLog PRIMARY KEY,
                            [IsDeleted] BIT NOT NULL CONSTRAINT DF_TelecomMsisdnChangeLog_IsDeleted DEFAULT (0),
                            [CreatedAtUtc] DATETIME2(7) NULL,
                            [CreatedById] NVARCHAR(450) NULL,
                            [UpdatedAtUtc] DATETIME2(7) NULL,
                            [UpdatedById] NVARCHAR(450) NULL,
                            [CustomerId] NVARCHAR(50) NOT NULL,
                            [SubscriberProfileId] NVARCHAR(50) NOT NULL,
                            [TelecomSubscriptionId] NVARCHAR(50) NOT NULL,
                            [MsisdnAssetId] NVARCHAR(50) NOT NULL,
                            [OldMsisdn] NVARCHAR(32) NULL,
                            [NewMsisdn] NVARCHAR(32) NULL,
                            [OldSubscriptionType] NVARCHAR(32) NULL,
                            [NewSubscriptionType] NVARCHAR(32) NULL,
                            [ExternalSyncSuccess] BIT NULL,
                            [ExternalSyncMessage] NVARCHAR(255) NULL,
                            CONSTRAINT FK_TelecomMsisdnChangeLog_Customer FOREIGN KEY ([CustomerId]) REFERENCES dbo.Customer ([Id])
                        );
                        CREATE INDEX IX_TelecomMsisdnChangeLog_CustomerId ON dbo.TelecomMsisdnChangeLog ([CustomerId]);
                        CREATE INDEX IX_TelecomMsisdnChangeLog_MsisdnAssetId ON dbo.TelecomMsisdnChangeLog ([MsisdnAssetId]);
                        CREATE INDEX IX_TelecomMsisdnChangeLog_CreatedAtUtc ON dbo.TelecomMsisdnChangeLog ([CreatedAtUtc]);
                    END
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var idx = connection.CreateCommand())
            {
                idx.CommandText = """
                    IF OBJECT_ID(N'dbo.MsisdnAsset', N'U') IS NULL RETURN;

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MsisdnAsset') AND name = N'IX_MsisdnAsset_Msisdn')
                        DROP INDEX IX_MsisdnAsset_Msisdn ON dbo.MsisdnAsset;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MsisdnAsset') AND name = N'IX_MsisdnAsset_Msisdn_ActiveOnly')
                    BEGIN
                        CREATE UNIQUE NONCLUSTERED INDEX IX_MsisdnAsset_Msisdn_ActiveOnly
                        ON dbo.MsisdnAsset ([Msisdn])
                        WHERE ([IsDeleted] = 0);
                    END
                    """;
                try
                {
                    idx.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Schema patch for MsisdnAsset filtered unique index skipped or failed.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch for TelecomMsisdnChangeLog / MSISDN index skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    /// Adds <c>TelecomSubscriptionTypes</c> reference data, migrates legacy <c>SubscriptionType</c> int to <c>SubscriptionTypeId</c> FK,
    /// and drops the old enum column when present. Canonical manual script: <c>Migrations/TelecomSubscriptionTypes_Manual.sql</c>.
    /// </summary>
    private static void ApplyTelecomSubscriptionTypeSchemaPatches(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var subTable = RelationalSchemaTableExists(dataContext, "TelecomSubscriptions")
            ? "TelecomSubscriptions"
            : RelationalSchemaTableExists(dataContext, "TelecomSubscription")
                ? "TelecomSubscription"
                : null;

        if (subTable == null)
        {
            logger.LogWarning("Telecom subscription table not found; subscription type schema patch skipped.");
            return;
        }

        var prepaid = TelecomSubscriptionTypeWellKnownIds.Prepaid;
        var postpaid = TelecomSubscriptionTypeWellKnownIds.Postpaid;
        var hybrid = TelecomSubscriptionTypeWellKnownIds.Hybrid;

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using (var bootstrap = connection.CreateCommand())
            {
                bootstrap.CommandText = $"""
                    IF OBJECT_ID(N'dbo.TelecomSubscriptionTypes', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.TelecomSubscriptionTypes (
                            [Id] NVARCHAR(50) NOT NULL CONSTRAINT PK_TelecomSubscriptionTypes PRIMARY KEY,
                            [IsDeleted] BIT NOT NULL CONSTRAINT DF_TelecomSubscriptionTypes_IsDeleted DEFAULT (0),
                            [CreatedAtUtc] DATETIME2(7) NULL,
                            [CreatedById] NVARCHAR(450) NULL,
                            [UpdatedAtUtc] DATETIME2(7) NULL,
                            [UpdatedById] NVARCHAR(450) NULL,
                            [Code] NVARCHAR(50) NOT NULL,
                            [NameAr] NVARCHAR(255) NOT NULL,
                            [NameEn] NVARCHAR(255) NOT NULL,
                            [DisplayColor] NVARCHAR(32) NULL,
                            [SortOrder] INT NOT NULL CONSTRAINT DF_TelecomSubscriptionTypes_SortOrder DEFAULT (0),
                            [IsActive] BIT NOT NULL CONSTRAINT DF_TelecomSubscriptionTypes_IsActive DEFAULT (1),
                            [IsDefault] BIT NOT NULL CONSTRAINT DF_TelecomSubscriptionTypes_IsDefault DEFAULT (0)
                        );
                    END
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'UX_TelecomSubscriptionTypes_Code' AND object_id = OBJECT_ID(N'dbo.TelecomSubscriptionTypes'))
                    BEGIN
                        CREATE UNIQUE NONCLUSTERED INDEX UX_TelecomSubscriptionTypes_Code
                        ON dbo.TelecomSubscriptionTypes([Code])
                        WHERE ([IsDeleted] = 0);
                    END
                    IF NOT EXISTS (SELECT 1 FROM dbo.TelecomSubscriptionTypes WHERE [Id] = N'{prepaid}')
                    INSERT INTO dbo.TelecomSubscriptionTypes
                        ([Id],[IsDeleted],[CreatedAtUtc],[Code],[NameAr],[NameEn],[DisplayColor],[SortOrder],[IsActive],[IsDefault])
                    VALUES
                        (N'{prepaid}', 0, SYSUTCDATETIME(), N'PREPAID', N'مسبق الدفع', N'Prepaid', N'#0d6efd', 1, 1, 1),
                        (N'{postpaid}', 0, SYSUTCDATETIME(), N'POSTPAID', N'آجل الدفع', N'Postpaid', N'#198754', 2, 1, 0),
                        (N'{hybrid}', 0, SYSUTCDATETIME(), N'HYBRID', N'هجين', N'Hybrid', N'#6f42c1', 3, 1, 0);
                    """;
                bootstrap.ExecuteNonQuery();
            }

            using (var alter = connection.CreateCommand())
            {
                alter.CommandText = $"""
                    IF COL_LENGTH(OBJECT_ID(N'dbo.{subTable}', N'U'), N'SubscriptionTypeId') IS NULL
                    BEGIN
                        ALTER TABLE dbo.[{subTable}] ADD [SubscriptionTypeId] NVARCHAR(50) NULL;
                    END
                    IF COL_LENGTH(OBJECT_ID(N'dbo.{subTable}', N'U'), N'SubscriptionType') IS NOT NULL
                    BEGIN
                        UPDATE dbo.[{subTable}]
                        SET [SubscriptionTypeId] = CASE [SubscriptionType]
                            WHEN 0 THEN N'{prepaid}'
                            WHEN 1 THEN N'{postpaid}'
                            ELSE N'{hybrid}'
                        END
                        WHERE [SubscriptionTypeId] IS NULL;
                    END
                    ELSE
                    BEGIN
                        UPDATE dbo.[{subTable}]
                        SET [SubscriptionTypeId] = N'{prepaid}'
                        WHERE [SubscriptionTypeId] IS NULL;
                    END
                    IF EXISTS (
                        SELECT 1 FROM sys.columns c
                        INNER JOIN sys.tables t ON c.object_id = t.object_id
                        WHERE SCHEMA_NAME(t.schema_id) = N'dbo' AND t.name = N'{subTable}'
                          AND c.name = N'SubscriptionTypeId' AND c.is_nullable = 1)
                    BEGIN
                        UPDATE dbo.[{subTable}] SET [SubscriptionTypeId] = N'{prepaid}' WHERE [SubscriptionTypeId] IS NULL;
                        ALTER TABLE dbo.[{subTable}] ALTER COLUMN [SubscriptionTypeId] NVARCHAR(50) NOT NULL;
                    END
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys fk
                        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.{subTable}')
                          AND fk.referenced_object_id = OBJECT_ID(N'dbo.TelecomSubscriptionTypes'))
                    BEGIN
                        ALTER TABLE dbo.[{subTable}]
                        ADD CONSTRAINT FK_{subTable}_TelecomSubscriptionTypes
                        FOREIGN KEY ([SubscriptionTypeId]) REFERENCES dbo.TelecomSubscriptionTypes ([Id]);
                    END
                    IF COL_LENGTH(OBJECT_ID(N'dbo.{subTable}', N'U'), N'SubscriptionType') IS NOT NULL
                    BEGIN
                        ALTER TABLE dbo.[{subTable}] DROP COLUMN [SubscriptionType];
                    END
                    IF COL_LENGTH(OBJECT_ID(N'dbo.TelecomMsisdnChangeLog', N'U'), N'OldSubscriptionType') IS NOT NULL
                    BEGIN
                        ALTER TABLE dbo.TelecomMsisdnChangeLog ALTER COLUMN [OldSubscriptionType] NVARCHAR(50) NULL;
                        ALTER TABLE dbo.TelecomMsisdnChangeLog ALTER COLUMN [NewSubscriptionType] NVARCHAR(50) NULL;
                    END
                    """;
                alter.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch for TelecomSubscriptionTypes / SubscriptionTypeId skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    /// Adds optional FK from Product to TelecomSubscriptionTypes for catalog / migration compatibility.
    /// Canonical manual script: <c>Migrations/Product_CompatibleSubscriptionType_Manual.sql</c>.
    /// </summary>
    private static void ApplyProductCompatibleSubscriptionTypeSchemaPatches(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var entityType = dataContext.Model.FindEntityType(typeof(Product));
        if (entityType == null)
        {
            return;
        }

        var tableName = entityType.GetTableName();
        if (string.IsNullOrEmpty(tableName))
        {
            return;
        }

        var schema = entityType.GetSchema();
        var schemaName = string.IsNullOrEmpty(schema) ? "dbo" : schema;

        if (!RelationalSchemaTableExists(dataContext, tableName))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            int colExists;
            using (var check = connection.CreateCommand())
            {
                check.CommandText = """
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = N'CompatibleSubscriptionTypeId'
                    """;
                var ps = check.CreateParameter();
                ps.ParameterName = "@schema";
                ps.Value = schemaName;
                check.Parameters.Add(ps);
                var pt = check.CreateParameter();
                pt.ParameterName = "@table";
                pt.Value = tableName;
                check.Parameters.Add(pt);
                colExists = Convert.ToInt32(check.ExecuteScalar() ?? 0);
            }

            if (colExists == 0)
            {
                logger.LogInformation(
                    "Applying schema patch: adding column CompatibleSubscriptionTypeId to {Schema}.{Table}.",
                    schemaName,
                    tableName);

                using var alter = connection.CreateCommand();
                alter.CommandText = $"""
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [CompatibleSubscriptionTypeId] NVARCHAR(50) NULL;
                    """;
                alter.ExecuteNonQuery();
            }

            using (var fk = connection.CreateCommand())
            {
                fk.CommandText = $"""
                    IF OBJECT_ID(N'[{schemaName}].[TelecomSubscriptionTypes]', N'U') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Product_CompatibleSubscriptionType')
                    BEGIN
                        ALTER TABLE [{schemaName}].[{tableName}]
                        ADD CONSTRAINT FK_Product_CompatibleSubscriptionType
                        FOREIGN KEY ([CompatibleSubscriptionTypeId]) REFERENCES [{schemaName}].[TelecomSubscriptionTypes]([Id]);
                    END
                    """;
                try
                {
                    fk.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Schema patch FK_Product_CompatibleSubscriptionType skipped or failed.");
                }
            }

            using var seed = connection.CreateCommand();
            seed.CommandText = $"""
                DECLARE @Pre NVARCHAR(50) = N'{TelecomSubscriptionTypeWellKnownIds.Prepaid}';
                DECLARE @Post NVARCHAR(50) = N'{TelecomSubscriptionTypeWellKnownIds.Postpaid}';
                DECLARE @Hyb NVARCHAR(50) = N'{TelecomSubscriptionTypeWellKnownIds.Hybrid}';
                UPDATE [{schemaName}].[{tableName}] SET [CompatibleSubscriptionTypeId] = @Pre
                    WHERE [IsDeleted] = 0 AND [ServiceCode] = N'YAHALA_SHABAB';
                UPDATE [{schemaName}].[{tableName}] SET [CompatibleSubscriptionTypeId] = @Hyb
                    WHERE [IsDeleted] = 0 AND [ServiceCode] = N'SYR_MIX_HYBRID';
                UPDATE [{schemaName}].[{tableName}] SET [CompatibleSubscriptionTypeId] = @Post
                    WHERE [IsDeleted] = 0 AND [ServiceCode] = N'SYR_POST_PLAT';
                UPDATE [{schemaName}].[{tableName}] SET [CompatibleSubscriptionTypeId] = NULL
                    WHERE [IsDeleted] = 0 AND (
                        [ServiceCode] IN (N'SABA_10GB', N'BUSINESS_PRO_50', N'NIGHT_UNL', N'HW_ROUTER_5G', N'HW_WINGLE', N'SRV_ACTIVATION', N'SRV_PROMO_DISC')
                        OR [ServiceCode] IS NULL OR LTRIM(RTRIM([ServiceCode])) = N''
                    );
                """;
            try
            {
                seed.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Schema patch seed updates for Product.CompatibleSubscriptionTypeId skipped or failed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch for Product.CompatibleSubscriptionTypeId skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyProductOfferingSmartFieldsPatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var entityType = dataContext.Model.FindEntityType(typeof(ProductOffering));
        if (entityType == null)
        {
            return;
        }

        var tableName = entityType.GetTableName();
        if (string.IsNullOrEmpty(tableName))
        {
            return;
        }

        var schema = entityType.GetSchema();
        var schemaName = string.IsNullOrEmpty(schema) ? "dbo" : schema;

        if (!RelationalSchemaTableExists(dataContext, tableName))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            logger.LogInformation("Applying schema patch: adding Smart Catalog columns to {Schema}.{Table}.", schemaName, tableName);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                IF COL_LENGTH(OBJECT_ID(N'[{schemaName}].[{tableName}]', N'U'), N'EligibilityRules') IS NULL
                BEGIN
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [EligibilityRules] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [AssetCompatibility] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [BillingCycle] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [TaxCategory] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [ServiceIdSocCode] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [SpeedQuotaLimitGb] FLOAT NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [VoiceMinutesLimit] INT NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [ThrottlingPolicy] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [IconClass] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [BadgeColor] NVARCHAR(100) NULL;
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [ShortDescription] NVARCHAR(500) NULL;
                END

                IF COL_LENGTH(OBJECT_ID(N'[{schemaName}].[{tableName}]', N'U'), N'PaymentType') IS NULL
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [PaymentType] INT NULL;

                IF COL_LENGTH(OBJECT_ID(N'[{schemaName}].[{tableName}]', N'U'), N'BillingCycleEnum') IS NULL
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [BillingCycleEnum] INT NULL;
                """;
            cmd.ExecuteNonQuery();

            ApplyProductOfferingComponentRequiresOfferingPatch(dataContext, connection, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch for Smart Catalog columns skipped or failed on {Schema}.{Table}.", schemaName, tableName);
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyProductOfferingComponentRequiresOfferingPatch(
        DataContext dataContext,
        System.Data.Common.DbConnection connection,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var componentEt = dataContext.Model.FindEntityType(typeof(ProductOfferingComponent));
        if (componentEt == null)
        {
            return;
        }

        var tableName = componentEt.GetTableName();
        if (string.IsNullOrEmpty(tableName) || !RelationalSchemaTableExists(dataContext, tableName))
        {
            return;
        }

        var schemaName = string.IsNullOrEmpty(componentEt.GetSchema()) ? "dbo" : componentEt.GetSchema()!;

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                IF COL_LENGTH(OBJECT_ID(N'[{schemaName}].[{tableName}]', N'U'), N'RequiresProductOfferingId') IS NULL
                    ALTER TABLE [{schemaName}].[{tableName}] ADD [RequiresProductOfferingId] NVARCHAR(450) NULL;
                """;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch: ProductOfferingComponent.RequiresProductOfferingId failed.");
        }
    }

    /// <summary>
    /// <c>EnsureCreated</c> does not add columns on existing DBs. Adds optional FK from commercial offerings to technical products
    /// and optional FK from telecom operations to the selected offering (wizard / catalog-driven flow).
    /// </summary>
    private static void ApplyProductOfferingProductIdAndOperationOfferingPatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var offeringEt = dataContext.Model.FindEntityType(typeof(ProductOffering));
        var productEt = dataContext.Model.FindEntityType(typeof(Product));
        var opEt = dataContext.Model.FindEntityType(typeof(TelecomOperationRequest));
        if (offeringEt == null || productEt == null || opEt == null)
        {
            return;
        }

        var offeringTable = offeringEt.GetTableName();
        var productTable = productEt.GetTableName();
        var opTable = opEt.GetTableName();
        if (string.IsNullOrEmpty(offeringTable) || string.IsNullOrEmpty(productTable) || string.IsNullOrEmpty(opTable))
        {
            return;
        }

        var schemaNameOffering = string.IsNullOrEmpty(offeringEt.GetSchema()) ? "dbo" : offeringEt.GetSchema()!;
        var schemaNameProduct = string.IsNullOrEmpty(productEt.GetSchema()) ? "dbo" : productEt.GetSchema()!;
        var schemaNameOp = string.IsNullOrEmpty(opEt.GetSchema()) ? "dbo" : opEt.GetSchema()!;

        if (!RelationalSchemaTableExists(dataContext, offeringTable)
            || !RelationalSchemaTableExists(dataContext, productTable)
            || !RelationalSchemaTableExists(dataContext, opTable))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            logger.LogInformation(
                "Applying schema patch: ProductOffering.ProductId and TelecomOperationRequest.ProductOfferingId on {Schema}.{Table}.",
                schemaNameOffering,
                offeringTable);

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"""
                    IF COL_LENGTH(OBJECT_ID(N'[{schemaNameOffering}].[{offeringTable}]', N'U'), N'ProductId') IS NULL
                    BEGIN
                        ALTER TABLE [{schemaNameOffering}].[{offeringTable}] ADD [ProductId] NVARCHAR(450) NULL;
                    END
                    """;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Schema patch: add ProductOffering.ProductId failed.");
            }

            try
            {
                using var fk = connection.CreateCommand();
                fk.CommandText = $"""
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys
                        WHERE name = N'FK_ProductOffering_Product_ProductId'
                          AND parent_object_id = OBJECT_ID(N'[{schemaNameOffering}].[{offeringTable}]'))
                    BEGIN
                        ALTER TABLE [{schemaNameOffering}].[{offeringTable}] WITH CHECK
                        ADD CONSTRAINT [FK_ProductOffering_Product_ProductId]
                            FOREIGN KEY ([ProductId]) REFERENCES [{schemaNameProduct}].[{productTable}] ([Id]);
                    END
                    """;
                fk.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Schema patch: FK_ProductOffering_Product_ProductId failed.");
            }

            try
            {
                using var cmdOp = connection.CreateCommand();
                cmdOp.CommandText = $"""
                    IF COL_LENGTH(OBJECT_ID(N'[{schemaNameOp}].[{opTable}]', N'U'), N'ProductOfferingId') IS NULL
                    BEGIN
                        ALTER TABLE [{schemaNameOp}].[{opTable}] ADD [ProductOfferingId] NVARCHAR(450) NULL;
                    END
                    """;
                cmdOp.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Schema patch: add TelecomOperationRequest.ProductOfferingId failed.");
            }

            try
            {
                using var fkOp = connection.CreateCommand();
                fkOp.CommandText = $"""
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys
                        WHERE name = N'FK_TelecomOperationRequest_ProductOffering_ProductOfferingId'
                          AND parent_object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]'))
                    BEGIN
                        ALTER TABLE [{schemaNameOp}].[{opTable}] WITH CHECK
                        ADD CONSTRAINT [FK_TelecomOperationRequest_ProductOffering_ProductOfferingId]
                            FOREIGN KEY ([ProductOfferingId]) REFERENCES [{schemaNameOffering}].[{offeringTable}] ([Id]);
                    END
                    """;
                fkOp.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Schema patch: FK_TelecomOperationRequest_ProductOffering_ProductOfferingId failed.");
            }
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    /// Older schemas enforced one <c>SubscriberProfile</c> per <c>Customer</c> via a unique index on <c>CustomerId</c>.
    /// Drops that unique index and ensures a non-unique index for party → many profiles (B2B / fleet).
    /// </summary>
    private static void ApplySubscriberProfileCustomerIndexNonUniquePatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (!RelationalSchemaTableExists(dataContext, "SubscriberProfile"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'dbo.SubscriberProfile', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM sys.indexes i
                        WHERE i.object_id = OBJECT_ID(N'dbo.SubscriberProfile')
                          AND i.name = N'IX_SubscriberProfile_CustomerId'
                          AND i.is_unique = 1)
                    BEGIN
                        DROP INDEX IX_SubscriberProfile_CustomerId ON dbo.SubscriberProfile;
                    END

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.indexes i
                        WHERE i.object_id = OBJECT_ID(N'dbo.SubscriberProfile')
                          AND i.name = N'IX_SubscriberProfile_CustomerId')
                    BEGIN
                        CREATE NONCLUSTERED INDEX IX_SubscriberProfile_CustomerId
                        ON dbo.SubscriberProfile ([CustomerId]);
                    END
                END
                """;
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Schema patch SubscriberProfile.CustomerId index (non-unique) skipped or failed.");
            }
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyDashboardWidgetsSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (RelationalSchemaTableExists(dataContext, "DashboardWidget"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'dbo.DashboardWidget', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.DashboardWidget (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_DashboardWidget_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        WidgetKey NVARCHAR(256) NOT NULL,
                        TitleAr NVARCHAR(256) NOT NULL,
                        TitleEn NVARCHAR(256) NULL,
                        Icon NVARCHAR(64) NULL,
                        ProviderKey NVARCHAR(256) NULL,
                        PersonasAllowed NVARCHAR(256) NOT NULL,
                        GridSize INT NOT NULL CONSTRAINT DF_DashboardWidget_GridSize DEFAULT 6,
                        SortOrder INT NOT NULL CONSTRAINT DF_DashboardWidget_SortOrder DEFAULT 0,
                        WidgetKind INT NOT NULL CONSTRAINT DF_DashboardWidget_WidgetKind DEFAULT 0,
                        RefreshIntervalSeconds INT NULL,
                        CtaUrl NVARCHAR(512) NULL,
                        CtaLabelAr NVARCHAR(256) NULL,
                        CtaLabelEn NVARCHAR(256) NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_DashboardWidget_IsActive DEFAULT 1
                    );
                    CREATE UNIQUE INDEX IX_DashboardWidget_WidgetKey ON dbo.DashboardWidget(WidgetKey) WHERE IsDeleted = 0;
                    CREATE INDEX IX_DashboardWidget_Active_Sort ON dbo.DashboardWidget(IsActive, SortOrder);
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: DashboardWidget table ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch DashboardWidget failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyTelecomTechnicalTicketSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (RelationalSchemaTableExists(dataContext, "TelecomTechnicalTicket"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'dbo.TelecomTechnicalTicket', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.TelecomTechnicalTicket (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_TelecomTechnicalTicket_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        TicketNumber NVARCHAR(64) NOT NULL,
                        Msisdn NVARCHAR(32) NOT NULL,
                        CustomerId NVARCHAR(450) NULL,
                        SubscriberProfileId NVARCHAR(450) NULL,
                        IssueType INT NOT NULL,
                        TicketCategory INT NOT NULL CONSTRAINT DF_TelecomTechnicalTicket_Category DEFAULT 0,
                        Priority INT NOT NULL,
                        Status INT NOT NULL,
                        AssignedToGroupId NVARCHAR(450) NULL,
                        OpenedByUserId NVARCHAR(450) NOT NULL,
                        ResolvedByUserId NVARCHAR(450) NULL,
                        Notes NVARCHAR(4000) NULL,
                        ResolutionNotes NVARCHAR(4000) NULL,
                        PayloadJson NVARCHAR(MAX) NULL,
                        ResolvedAtUtc DATETIME2 NULL,
                        CreatedByChannel NVARCHAR(64) NOT NULL CONSTRAINT DF_TelecomTechnicalTicket_Channel DEFAULT 'CallCenter_Agent'
                    );
                    CREATE UNIQUE INDEX IX_TelecomTechnicalTicket_TicketNumber ON dbo.TelecomTechnicalTicket(TicketNumber);
                    CREATE INDEX IX_TelecomTechnicalTicket_Msisdn ON dbo.TelecomTechnicalTicket(Msisdn);
                    CREATE INDEX IX_TelecomTechnicalTicket_Status ON dbo.TelecomTechnicalTicket(Status);
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: TelecomTechnicalTicket table ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch TelecomTechnicalTicket failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyTelecomTechnicalTicketCreatedByChannelPatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (!RelationalSchemaTableExists(dataContext, "TelecomTechnicalTicket"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TelecomTechnicalTicket' AND COLUMN_NAME = 'CreatedByChannel')
                BEGIN
                    ALTER TABLE dbo.TelecomTechnicalTicket
                        ADD CreatedByChannel NVARCHAR(64) NOT NULL
                        CONSTRAINT DF_TelecomTechnicalTicket_CreatedByChannel DEFAULT 'CallCenter_Agent';
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: TelecomTechnicalTicket.CreatedByChannel ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch TelecomTechnicalTicket.CreatedByChannel failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyTelecomTechnicalTicketCategoryPatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (!RelationalSchemaTableExists(dataContext, "TelecomTechnicalTicket"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TelecomTechnicalTicket' AND COLUMN_NAME = 'TicketCategory')
                BEGIN
                    ALTER TABLE dbo.TelecomTechnicalTicket
                        ADD TicketCategory INT NOT NULL
                        CONSTRAINT DF_TelecomTechnicalTicket_TicketCategory DEFAULT 0;
                    CREATE INDEX IX_TelecomTechnicalTicket_TicketCategory ON dbo.TelecomTechnicalTicket(TicketCategory);
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: TelecomTechnicalTicket.TicketCategory ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch TelecomTechnicalTicket.TicketCategory failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyMsisdnAssetPairedKitSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (!RelationalSchemaTableExists(dataContext, "MsisdnAsset"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'MsisdnAsset' AND COLUMN_NAME = 'PairedIccid')
                BEGIN
                    ALTER TABLE dbo.MsisdnAsset ADD PairedIccid NVARCHAR(32) NULL;
                END
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'MsisdnAsset' AND COLUMN_NAME = 'PairedImsi')
                BEGIN
                    ALTER TABLE dbo.MsisdnAsset ADD PairedImsi NVARCHAR(32) NULL;
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: MsisdnAsset.PairedIccid/PairedImsi ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch MsisdnAsset paired kit columns failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyTelecomIntegrationLogSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SET QUOTED_IDENTIFIER ON;
                IF OBJECT_ID(N'dbo.TelecomIntegrationLog', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.TelecomIntegrationLog (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_TelecomIntegrationLog_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        Msisdn NVARCHAR(256) NULL,
                        IntegrationSystem INT NOT NULL,
                        OperationName NVARCHAR(256) NOT NULL,
                        RequestPayload NVARCHAR(MAX) NULL,
                        ResponsePayload NVARCHAR(MAX) NULL,
                        ExecutionTimeMs BIGINT NOT NULL,
                        IsSuccess BIT NOT NULL,
                        ResponseStatusCode NVARCHAR(64) NULL,
                        OccurredAtUtc DATETIME2 NOT NULL
                    );
                END
                IF OBJECT_ID(N'dbo.TelecomIntegrationLog', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TelecomIntegrationLog_Msisdn' AND object_id = OBJECT_ID(N'dbo.TelecomIntegrationLog'))
                        CREATE INDEX IX_TelecomIntegrationLog_Msisdn ON dbo.TelecomIntegrationLog (Msisdn) WHERE Msisdn IS NOT NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TelecomIntegrationLog_IntegrationSystem' AND object_id = OBJECT_ID(N'dbo.TelecomIntegrationLog'))
                        CREATE INDEX IX_TelecomIntegrationLog_IntegrationSystem ON dbo.TelecomIntegrationLog (IntegrationSystem);
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TelecomIntegrationLog_OccurredAtUtc' AND object_id = OBJECT_ID(N'dbo.TelecomIntegrationLog'))
                        CREATE INDEX IX_TelecomIntegrationLog_OccurredAtUtc ON dbo.TelecomIntegrationLog (OccurredAtUtc DESC);
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: TelecomIntegrationLog table ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch TelecomIntegrationLog failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyVasCatalogSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (RelationalSchemaTableExists(dataContext, "TelecomValueAddedService"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'dbo.TelecomValueAddedService', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.TelecomValueAddedService (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_TelecomVAS_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        ServiceCode NVARCHAR(64) NOT NULL,
                        NameAr NVARCHAR(255) NOT NULL,
                        NameEn NVARCHAR(255) NULL,
                        Description NVARCHAR(2000) NULL,
                        MonthlyFee DECIMAL(18,2) NOT NULL CONSTRAINT DF_TelecomVAS_MonthlyFee DEFAULT 0,
                        IsActive BIT NOT NULL CONSTRAINT DF_TelecomVAS_IsActive DEFAULT 1,
                        HlrCommandTemplate NVARCHAR(512) NOT NULL,
                        SortOrder INT NOT NULL CONSTRAINT DF_TelecomVAS_SortOrder DEFAULT 0
                    );
                    CREATE UNIQUE INDEX IX_TelecomValueAddedService_ServiceCode ON dbo.TelecomValueAddedService(ServiceCode);
                    CREATE INDEX IX_TelecomValueAddedService_IsActive ON dbo.TelecomValueAddedService(IsActive);
                END

                IF OBJECT_ID(N'dbo.SubscriberActiveService', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.SubscriberActiveService (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_SubscriberActiveService_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        TelecomSubscriptionId NVARCHAR(50) NOT NULL,
                        TelecomValueAddedServiceId NVARCHAR(50) NOT NULL,
                        Msisdn NVARCHAR(32) NOT NULL,
                        Status INT NOT NULL CONSTRAINT DF_SubscriberActiveService_Status DEFAULT 0,
                        ActivatedAtUtc DATETIME2 NULL,
                        DeactivatedAtUtc DATETIME2 NULL
                    );
                    CREATE UNIQUE INDEX IX_SubscriberActiveService_Subscription_Service
                        ON dbo.SubscriberActiveService(TelecomSubscriptionId, TelecomValueAddedServiceId);
                    CREATE INDEX IX_SubscriberActiveService_Msisdn ON dbo.SubscriberActiveService(Msisdn);
                    IF OBJECT_ID(N'dbo.TelecomSubscription', N'U') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SubscriberActiveService_TelecomSubscription')
                    BEGIN
                        ALTER TABLE dbo.SubscriberActiveService
                        ADD CONSTRAINT FK_SubscriberActiveService_TelecomSubscription
                        FOREIGN KEY (TelecomSubscriptionId) REFERENCES dbo.TelecomSubscription(Id) ON DELETE CASCADE;
                    END
                    IF OBJECT_ID(N'dbo.TelecomValueAddedService', N'U') IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SubscriberActiveService_TelecomVAS')
                    BEGIN
                        ALTER TABLE dbo.SubscriberActiveService
                        ADD CONSTRAINT FK_SubscriberActiveService_TelecomVAS
                        FOREIGN KEY (TelecomValueAddedServiceId) REFERENCES dbo.TelecomValueAddedService(Id);
                    END
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: VAS catalog tables ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch VAS catalog failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyBulkImportEnterpriseSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryBulkImportJob')
                BEGIN
                    CREATE TABLE dbo.InventoryBulkImportJob (
                        Id NVARCHAR(50) NOT NULL PRIMARY KEY,
                        JobStatus INT NOT NULL CONSTRAINT DF_InvBulkJob_Status DEFAULT 0,
                        JobType INT NOT NULL CONSTRAINT DF_InvBulkJob_Type DEFAULT 0,
                        FileName NVARCHAR(512) NULL,
                        StoredFilePath NVARCHAR(1024) NULL,
                        TotalRows INT NOT NULL CONSTRAINT DF_InvBulkJob_Total DEFAULT 0,
                        ProcessedRows INT NOT NULL CONSTRAINT DF_InvBulkJob_Processed DEFAULT 0,
                        SuccessCount INT NOT NULL CONSTRAINT DF_InvBulkJob_Success DEFAULT 0,
                        ErrorCount INT NOT NULL CONSTRAINT DF_InvBulkJob_Errors DEFAULT 0,
                        ErrorSummary NVARCHAR(4000) NULL,
                        PayloadJson NVARCHAR(MAX) NULL,
                        StartedAtUtc DATETIME2 NULL,
                        CompletedAtUtc DATETIME2 NULL,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_InvBulkJob_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(50) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(50) NULL
                    );
                    CREATE INDEX IX_InvBulkJob_Status ON dbo.InventoryBulkImportJob (JobStatus);
                    CREATE INDEX IX_InvBulkJob_Type ON dbo.InventoryBulkImportJob (JobType);
                    CREATE INDEX IX_InvBulkJob_CreatedBy ON dbo.InventoryBulkImportJob (CreatedById);
                    CREATE INDEX IX_InvBulkJob_CreatedAt ON dbo.InventoryBulkImportJob (CreatedAtUtc);
                END
                ELSE
                BEGIN
                    IF COL_LENGTH('dbo.InventoryBulkImportJob', 'JobType') IS NULL
                        ALTER TABLE dbo.InventoryBulkImportJob ADD JobType INT NOT NULL CONSTRAINT DF_InvBulkJob_Type_Existing DEFAULT 0;
                    IF COL_LENGTH('dbo.InventoryBulkImportJob', 'FileName') IS NULL
                        ALTER TABLE dbo.InventoryBulkImportJob ADD FileName NVARCHAR(512) NULL;
                    IF COL_LENGTH('dbo.InventoryBulkImportJob', 'StoredFilePath') IS NULL
                        ALTER TABLE dbo.InventoryBulkImportJob ADD StoredFilePath NVARCHAR(1024) NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvBulkJob_Type' AND object_id = OBJECT_ID('dbo.InventoryBulkImportJob'))
                        CREATE INDEX IX_InvBulkJob_Type ON dbo.InventoryBulkImportJob (JobType);
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvBulkJob_CreatedBy' AND object_id = OBJECT_ID('dbo.InventoryBulkImportJob'))
                        CREATE INDEX IX_InvBulkJob_CreatedBy ON dbo.InventoryBulkImportJob (CreatedById);
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvBulkJob_CreatedAt' AND object_id = OBJECT_ID('dbo.InventoryBulkImportJob'))
                        CREATE INDEX IX_InvBulkJob_CreatedAt ON dbo.InventoryBulkImportJob (CreatedAtUtc);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InventoryBulkImportError')
                BEGIN
                    CREATE TABLE dbo.InventoryBulkImportError (
                        Id NVARCHAR(50) NOT NULL PRIMARY KEY,
                        JobId NVARCHAR(50) NOT NULL,
                        RowNumber INT NOT NULL,
                        Identifier NVARCHAR(128) NULL,
                        ErrorMessageAr NVARCHAR(2000) NULL,
                        ErrorMessageEn NVARCHAR(2000) NULL,
                        RawRowDataJson NVARCHAR(4000) NULL,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_InvBulkErr_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(50) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(50) NULL,
                        CONSTRAINT FK_InvBulkErr_Job FOREIGN KEY (JobId) REFERENCES dbo.InventoryBulkImportJob(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_InvBulkErr_JobId ON dbo.InventoryBulkImportError (JobId);
                    CREATE INDEX IX_InvBulkErr_JobRow ON dbo.InventoryBulkImportError (JobId, RowNumber);
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: Bulk import enterprise (job columns + error table) ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch Bulk import enterprise failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyRolePermissionSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (RelationalSchemaTableExists(dataContext, "RolePermission"))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'dbo.RolePermission', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.RolePermission (
                        RoleName NVARCHAR(256) NOT NULL,
                        PermissionKey NVARCHAR(128) NOT NULL,
                        GrantedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_RolePermission_GrantedAtUtc DEFAULT SYSUTCDATETIME(),
                        GrantedById NVARCHAR(450) NULL,
                        CONSTRAINT PK_RolePermission PRIMARY KEY (RoleName, PermissionKey)
                    );
                    CREATE INDEX IX_RolePermission_PermissionKey ON dbo.RolePermission(PermissionKey);
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: RolePermission table ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch RolePermission skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyAdministrationFoundationSchemaPatches(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'dbo.GlobalSetting', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.GlobalSetting (
                        [Key] NVARCHAR(128) NOT NULL PRIMARY KEY,
                        Value NVARCHAR(4000) NOT NULL CONSTRAINT DF_GlobalSetting_Value DEFAULT '',
                        Category NVARCHAR(64) NULL,
                        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_GlobalSetting_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
                        UpdatedById NVARCHAR(450) NULL
                    );
                END

                IF OBJECT_ID(N'dbo.OrgUnit', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.OrgUnit (
                        Id NVARCHAR(50) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_OrgUnit_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        ParentId NVARCHAR(50) NULL,
                        NameAr NVARCHAR(256) NOT NULL,
                        NameEn NVARCHAR(256) NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_OrgUnit_IsActive DEFAULT 1
                    );
                    CREATE INDEX IX_OrgUnit_ParentId ON dbo.OrgUnit(ParentId);
                END

                IF COL_LENGTH(N'dbo.OrgUnit', N'ManagerUserId') IS NULL
                    ALTER TABLE dbo.OrgUnit ADD ManagerUserId NVARCHAR(450) NULL;
                IF COL_LENGTH(N'dbo.OrgUnit', N'Kind') IS NULL
                    ALTER TABLE dbo.OrgUnit ADD Kind INT NOT NULL CONSTRAINT DF_OrgUnit_Kind DEFAULT 2;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrgUnit_ManagerUserId' AND object_id = OBJECT_ID(N'dbo.OrgUnit'))
                    CREATE INDEX IX_OrgUnit_ManagerUserId ON dbo.OrgUnit(ManagerUserId);

                IF COL_LENGTH(N'dbo.Customer', N'OrgUnitId') IS NULL
                    ALTER TABLE dbo.Customer ADD OrgUnitId NVARCHAR(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customer_OrgUnitId' AND object_id = OBJECT_ID(N'dbo.Customer'))
                    CREATE INDEX IX_Customer_OrgUnitId ON dbo.Customer(OrgUnitId);

                IF OBJECT_ID(N'dbo.UserAuditLog', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.UserAuditLog (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_UserAuditLog_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        UserId NVARCHAR(450) NULL,
                        ActorUserId NVARCHAR(450) NOT NULL,
                        ActionType NVARCHAR(64) NOT NULL,
                        EntityType NVARCHAR(128) NULL,
                        EntityId NVARCHAR(450) NULL,
                        SummaryAr NVARCHAR(512) NULL,
                        PayloadJson NVARCHAR(MAX) NULL,
                        OccurredAtUtc DATETIME2 NOT NULL,
                        IpAddress NVARCHAR(64) NULL
                    );
                    CREATE INDEX IX_UserAuditLog_UserId ON dbo.UserAuditLog(UserId);
                    CREATE INDEX IX_UserAuditLog_ActorUserId ON dbo.UserAuditLog(ActorUserId);
                    CREATE INDEX IX_UserAuditLog_OccurredAtUtc ON dbo.UserAuditLog(OccurredAtUtc);
                END

                IF COL_LENGTH(N'dbo.AspNetUsers', N'PrimaryMenuPersona') IS NULL
                    ALTER TABLE dbo.AspNetUsers ADD PrimaryMenuPersona INT NULL;
                IF COL_LENGTH(N'dbo.AspNetUsers', N'ManagerUserId') IS NULL
                    ALTER TABLE dbo.AspNetUsers ADD ManagerUserId NVARCHAR(450) NULL;
                IF COL_LENGTH(N'dbo.AspNetUsers', N'OrgUnitId') IS NULL
                    ALTER TABLE dbo.AspNetUsers ADD OrgUnitId NVARCHAR(50) NULL;
                IF COL_LENGTH(N'dbo.AspNetUsers', N'LastLoginAtUtc') IS NULL
                    ALTER TABLE dbo.AspNetUsers ADD LastLoginAtUtc DATETIME2 NULL;
                IF COL_LENGTH(N'dbo.AspNetUsers', N'LastActivityAtUtc') IS NULL
                    ALTER TABLE dbo.AspNetUsers ADD LastActivityAtUtc DATETIME2 NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUsers_ManagerUserId' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
                    CREATE INDEX IX_AspNetUsers_ManagerUserId ON dbo.AspNetUsers(ManagerUserId);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUsers_OrgUnitId' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
                    CREATE INDEX IX_AspNetUsers_OrgUnitId ON dbo.AspNetUsers(OrgUnitId);
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: Administration foundation (GlobalSetting, OrgUnit, UserAuditLog, AspNetUsers columns) ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch Administration foundation skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
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


