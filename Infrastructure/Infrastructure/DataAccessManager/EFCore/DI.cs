using System.Data;
using Application.Common.CQS.Commands;
using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.DataAccessManager.EFCore.Repositories;
using Infrastructure.DataAccessManager.EFCore.SchemaPatches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"],
            "Development",
            StringComparison.OrdinalIgnoreCase);

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
                    ConfigureSqlServer(options, connectionString, isDevelopment));
                services.AddDbContext<CommandContext>(options =>
                    ConfigureSqlServer(options, connectionString, isDevelopment));
                services.AddDbContext<QueryContext>(options =>
                    ConfigureSqlServer(options, connectionString, isDevelopment));
                break;
        }


        services.AddScoped<ICommandContext, CommandContext>();
        services.AddScoped<IQueryContext, QueryContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));


        return services;
    }

    private static void ConfigureSqlServer(
        DbContextOptionsBuilder options,
        string? connectionString,
        bool isDevelopment)
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(2),
            errorNumbersToAdd: null));

        if (isDevelopment)
        {
            options.LogTo(Log.Information, LogLevel.Information)
                .EnableSensitiveDataLogging();
        }
    }

    public static IHost CreateDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DI));

        if (!configuration.GetValue("Database:UseEfMigrations", true))
        {
            throw new InvalidOperationException(
                "Database:UseEfMigrations must be true. EnsureCreated bootstrap was removed — use EF Core migrations only.");
        }

        using (serviceProvider.GetRequiredService<Application.Common.Security.ISystemExecutionGate>().Enter())
        {
            var dataContext = serviceProvider.GetRequiredService<DataContext>();

            BaselineLegacyEfMigrationsIfNeeded(dataContext, logger);
            logger.LogInformation("Applying EF Core migrations (Database:UseEfMigrations=true).");
            dataContext.Database.Migrate();

            SchemaPatches.RlsBranchIdSchemaPatches.EnsureBranchIdColumns(dataContext, logger);

            if (configuration.GetValue("Database:ApplyLegacyPatchesAfterMigrations", false))
            {
                logger.LogWarning("Applying one-time legacy schema patches (upgrade path only).");
                ApplyIntegrationInfrastructureSchemaPatch(dataContext, logger);
                ApplyRowVersionColumnsSchemaPatch(dataContext, logger);
                ApplyTelecomTechnicalTicketBranchIdPatch(dataContext, logger);
            }
        }

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
                    EXEC sp_executesql N'UPDATE dbo.TelecomOperationRequest SET IsLostOrStolenReport = 0 WHERE IsLostOrStolenReport IS NULL';
                    EXEC sp_executesql N'ALTER TABLE dbo.TelecomOperationRequest ALTER COLUMN IsLostOrStolenReport bit NOT NULL';
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
                        EXEC sp_executesql N'DROP INDEX IX_SubscriberProfile_CommercialRegistration ON dbo.SubscriberProfile';

                    IF EXISTS (
                        SELECT 1 FROM sys.indexes i
                        WHERE i.object_id = OBJECT_ID(N'dbo.SubscriberProfile')
                          AND i.name = N'IX_SubscriberProfile_NationalId')
                        EXEC sp_executesql N'DROP INDEX IX_SubscriberProfile_NationalId ON dbo.SubscriberProfile';

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
                EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{tableName}] ADD [TargetOfferName] NVARCHAR(255) NULL';
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
                EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{tableName}] ADD [IdentityDocumentStorageKey] NVARCHAR(500) NULL';
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

    private static void ApplyTelecomOperationKycDocumentReferenceSchemaPatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
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
                    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = N'KycDocumentReferenceId'
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
                "Applying schema patch: adding column KycDocumentReferenceId to {Schema}.{Table}.",
                schemaName,
                tableName);

            using var alter = connection.CreateCommand();
            alter.CommandText = $"""
                EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{tableName}] ADD [KycDocumentReferenceId] NVARCHAR(50) NULL';
                """;
            alter.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Schema patch for KycDocumentReferenceId skipped or failed on {Schema}.{Table}.",
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

    private static void ApplyTelecomOperationComprehensivePatch(
        DataContext dataContext,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var entityType = dataContext.Model.FindEntityType(typeof(TelecomOperationRequest));
        if (entityType == null) return;

        var tableName = entityType.GetTableName();
        if (string.IsNullOrEmpty(tableName)) return;

        var schema = entityType.GetSchema();
        var schemaName = string.IsNullOrEmpty(schema) ? "dbo" : schema;
        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen) connection.Open();

        try
        {
            var columns = new List<(string Name, string Type)>
            {
                // §8 Suspension
                ("SuspensionType", "NVARCHAR(32) NULL"),
                ("SuspensionReason", "NVARCHAR(256) NULL"),
                ("SuspensionStartDateUtc", "DATETIME2 NULL"),
                ("SuspensionEndDateUtc", "DATETIME2 NULL"),
                ("BarringLevel", "NVARCHAR(32) NULL"),
                ("AutoReconnectEnabled", "BIT NOT NULL DEFAULT 0"),
                ("NotificationSuppressed", "BIT NOT NULL DEFAULT 0"),
                ("BarStatus", "NVARCHAR(64) NULL"),
                ("PriorOperationalStatus", "NVARCHAR(32) NULL"),

                // §9 Reconnect
                ("ReconnectReason", "NVARCHAR(256) NULL"),
                ("ClearanceType", "NVARCHAR(32) NULL"),
                ("SourceSuspensionOperationId", "NVARCHAR(128) NULL"),
                ("FraudClearanceConfirmed", "BIT NOT NULL DEFAULT 0"),
                ("FraudClearanceByUserId", "NVARCHAR(128) NULL"),
                ("ReactivationAtUtc", "DATETIME2 NULL"),
                ("ProvisioningResult", "NVARCHAR(64) NULL"),

                // §7 Take-Over
                ("TransferReason", "NVARCHAR(256) NULL"),
                ("TakeOverObligationStatus", "NVARCHAR(64) NULL"),
                ("DepositTransferPolicy", "INT NULL"),
                ("ApprovalLevelRequired", "NVARCHAR(32) NULL"),
                ("TakeOverEffectiveDateUtc", "DATETIME2 NULL"),
                ("OldCustomerId", "NVARCHAR(50) NULL"),
                ("NewCustomerId", "NVARCHAR(50) NULL"),
                ("PriorSubscriberProfileId", "NVARCHAR(50) NULL"),

                // §10 Termination
                ("TerminationType", "NVARCHAR(32) NULL"),
                ("TerminationReason", "NVARCHAR(256) NULL"),
                ("TerminationEffectiveDateUtc", "DATETIME2 NULL"),
                ("FinalBillAmount", "DECIMAL(18,2) NULL"),
                ("DepositSettlementAmount", "DECIMAL(18,2) NULL"),
                ("DepositSettlementStatus", "NVARCHAR(64) NULL"),
                ("RetentionOfferOutcome", "NVARCHAR(128) NULL"),
                ("DeprovisionStatus", "NVARCHAR(64) NULL"),

                // §15 Refund
                ("RefundType", "NVARCHAR(32) NULL"),
                ("RefundReason", "NVARCHAR(256) NULL"),
                ("RefundAmount", "DECIMAL(18,2) NULL"),
                ("RefundMethod", "NVARCHAR(32) NULL"),
                ("DepositBalanceSnapshot", "DECIMAL(18,2) NULL"),
                ("WalletBalanceSnapshot", "DECIMAL(18,2) NULL"),
                ("RefundSettlementStatus", "NVARCHAR(32) NULL"),
                ("RefundCbsReference", "NVARCHAR(128) NULL"),
                ("RefundGatewayReference", "NVARCHAR(128) NULL"),
                ("RequiresDualApproval", "BIT NOT NULL DEFAULT 0"),

                // §16 BDR
                ("CollectionAction", "NVARCHAR(32) NULL"),
                ("DunningStage", "NVARCHAR(32) NULL"),
                ("PriorDunningStage", "NVARCHAR(32) NULL"),
                ("OutstandingBalanceSnapshot", "DECIMAL(18,2) NULL"),
                ("CollectedAmount", "DECIMAL(18,2) NULL"),
                ("WriteOffAmount", "DECIMAL(18,2) NULL"),
                ("AgencyReference", "NVARCHAR(128) NULL"),
                ("PaymentPlanMonths", "INT NULL"),
                ("NextDunningDueUtc", "DATETIME2 NULL"),
                ("CollectionNote", "NVARCHAR(512) NULL"),
                ("CollectionSettlementStatus", "NVARCHAR(32) NULL"),

                // §14 Device Sales
                ("DeviceInventoryId", "NVARCHAR(50) NULL"),
                ("DeviceSaleType", "INT NULL"),
                ("InstallmentPlanId", "NVARCHAR(50) NULL"),
                ("DeviceDownPaymentAmount", "DECIMAL(18,2) NULL"),
                ("DeviceMonthlyInstallmentAmount", "DECIMAL(18,2) NULL"),
                ("DeviceCreditScoreSnapshot", "INT NULL"),
                ("DeviceInstallmentContractId", "NVARCHAR(50) NULL"),
                ("DeviceFinancingDecision", "INT NULL"),
                ("DeviceOverrideReasonCode", "NVARCHAR(64) NULL"),
                ("DeviceApprovalLevelRequired", "NVARCHAR(64) NULL"),
                ("DeviceFinancingNoteAr", "NVARCHAR(512) NULL"),
                ("DeviceWarrantyStartsAtUtc", "DATETIME2 NULL"),

                // §5 Change Number
                ("PriorMsisdnAssetId", "NVARCHAR(50) NULL"),
                ("TargetMsisdnAssetId", "NVARCHAR(50) NULL"),
                ("NumberChangeReason", "NVARCHAR(256) NULL"),
                ("PremiumFeeAmount", "DECIMAL(18,2) NULL"),
                ("NumberChangeMode", "NVARCHAR(32) NULL"),
                ("NumberChangeEffectiveDateUtc", "DATETIME2 NULL"),
                ("PortInMsisdn", "NVARCHAR(32) NULL"),
                ("DonorOperatorCode", "NVARCHAR(32) NULL"),

                // §6 Change GSM
                ("SourceSubscriptionTypeId", "NVARCHAR(50) NULL"),
                ("TargetSubscriptionTypeId", "NVARCHAR(50) NULL"),
                ("GsmMigrationReason", "NVARCHAR(256) NULL"),
                ("GsmEffectiveDateUtc", "DATETIME2 NULL"),
                ("GsmCompatibilityStatus", "NVARCHAR(64) NULL"),

                // §4 SIM Swap
                ("ReplacementReason", "NVARCHAR(256) NULL"),
                ("SimSwapEffectiveDateUtc", "DATETIME2 NULL"),
                ("IsLostOrStolenReport", "BIT NOT NULL DEFAULT 0"),
                ("PriorSimInventoryId", "NVARCHAR(50) NULL"),

                // Selling Line
                ("ActivationChannel", "INT NOT NULL DEFAULT 0"),
                ("DealerCode", "NVARCHAR(64) NULL"),
                ("BranchId", "NVARCHAR(50) NULL"),
                ("PaymentReference", "NVARCHAR(128) NULL"),
                ("InitialDepositAmount", "DECIMAL(18,2) NULL"),
                ("OverrideReasonCode", "NVARCHAR(64) NULL"),
                ("KycVerifiedAtUtc", "DATETIME2 NULL"),

                // SLA & Queue
                ("SlaExpirationTimeUtc", "DATETIME2(7) NULL"),
                ("ClaimedByUserId", "NVARCHAR(450) NULL"),
                ("AssignedAgentEmail", "NVARCHAR(128) NULL"),
                ("ClaimedAt", "DATETIME2 NULL")
            };

            foreach (var col in columns)
            {
                using var check = connection.CreateCommand();
                check.CommandText = $"""
                    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = '{schemaName}' AND TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{col.Name}'
                    """;
                var exists = Convert.ToInt32(check.ExecuteScalar() ?? 0) > 0;
                if (!exists)
                {
                    logger.LogInformation("Applying schema patch: adding column {Column} to {Schema}.{Table}.", col.Name, schemaName, tableName);
                    using var alter = connection.CreateCommand();
                    alter.CommandText = $"EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{tableName}] ADD [{col.Name}] {col.Type}';";
                    alter.ExecuteNonQuery();
                }
                else
                {
                    // Ensure correct length for Id-referencing columns if they already exist with wrong length
                    if (col.Type.Contains("NVARCHAR(50)"))
                    {
                        using var lengthCheck = connection.CreateCommand();
                        lengthCheck.CommandText = $"""
                            SELECT max_length FROM sys.columns 
                            WHERE name = '{col.Name}' AND object_id = OBJECT_ID('[{schemaName}].[{tableName}]')
                            """;
                        var maxLength = Convert.ToInt32(lengthCheck.ExecuteScalar() ?? 0);
                        if (maxLength != 100) // 50 * 2 for NVARCHAR
                        {
                            logger.LogInformation("Applying schema patch: correcting column length for {Column} in {Schema}.{Table}.", col.Name, schemaName, tableName);
                            using var alter = connection.CreateCommand();
                            alter.CommandText = $"EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{tableName}] ALTER COLUMN [{col.Name}] {col.Type}';";
                            alter.ExecuteNonQuery();
                        }
                    }
                }
            }

            // Indexes for TelecomOperationRequest
            var indexes = new List<(string Name, string Definition)>
            {
                ("IX_TelecomOperationRequest_Suspension", "(Kind, CreatedAtUtc DESC) WHERE Kind = 8 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_Reconnect", "(Kind, CreatedAtUtc DESC) WHERE Kind = 9 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_TakeOver", "(Kind, CreatedAtUtc DESC) WHERE Kind = 2 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_Termination", "(Kind, CreatedAtUtc DESC) WHERE Kind = 7 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_Refund", "(Kind, CreatedAtUtc DESC) WHERE Kind = 11 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_DeviceSale", "(Kind, CreatedAtUtc DESC) WHERE Kind = 10 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_ChangeNumber", "(Kind, CreatedAtUtc DESC) WHERE Kind = 5 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_ChangeGsm", "(Kind, CreatedAtUtc DESC) WHERE Kind = 6 AND IsDeleted = 0"),
                ("IX_TelecomOperationRequest_SimSwap", "(Kind, CreatedAtUtc DESC) WHERE Kind = 3 AND IsDeleted = 0")
            };

            foreach (var idx in indexes)
            {
                using var check = connection.CreateCommand();
                check.CommandText = $"SELECT COUNT(*) FROM sys.indexes WHERE name = '{idx.Name}' AND object_id = OBJECT_ID('[{schemaName}].[{tableName}]')";
                var exists = Convert.ToInt32(check.ExecuteScalar() ?? 0) > 0;
                if (!exists)
                {
                    logger.LogInformation("Applying schema patch: adding index {Index} to {Schema}.{Table}.", idx.Name, schemaName, tableName);
                    using var alter = connection.CreateCommand();
                    alter.CommandText = $"EXEC sp_executesql N'CREATE INDEX [{idx.Name}] ON [{schemaName}].[{tableName}] {idx.Definition}';";
                    alter.ExecuteNonQuery();
                }
            }

            // Audit Log Patches
            var auditEntityType = dataContext.Model.FindEntityType(typeof(TelecomOperationAuditLog));
            if (auditEntityType != null)
            {
                var auditTable = auditEntityType.GetTableName();
                if (!string.IsNullOrEmpty(auditTable))
                {
                    var auditColumns = new List<(string Name, string Type)>
                    {
                        ("ActivationChannel", "INT NULL"),
                        ("BranchId", "NVARCHAR(50) NULL"),
                        ("DealerCode", "NVARCHAR(64) NULL"),
                        ("OverrideReasonCode", "NVARCHAR(64) NULL"),
                        ("CorrelationId", "NVARCHAR(450) NULL"),
                        ("FieldChangesJson", "NVARCHAR(MAX) NULL")
                    };

                    foreach (var col in auditColumns)
                    {
                        using var check = connection.CreateCommand();
                        check.CommandText = $"""
                            SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_SCHEMA = '{schemaName}' AND TABLE_NAME = '{auditTable}' AND COLUMN_NAME = '{col.Name}'
                            """;
                        var exists = Convert.ToInt32(check.ExecuteScalar() ?? 0) > 0;
                        if (!exists)
                        {
                            logger.LogInformation("Applying schema patch: adding column {Column} to {Schema}.{Table}.", col.Name, schemaName, auditTable);
                            using var alter = connection.CreateCommand();
                            alter.CommandText = $"EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{auditTable}] ADD [{col.Name}] {col.Type}';";
                            alter.ExecuteNonQuery();
                        }
                        else
                        {
                            if (col.Type.Contains("NVARCHAR(50)"))
                            {
                                using var lengthCheck = connection.CreateCommand();
                                lengthCheck.CommandText = $"""
                                    SELECT max_length FROM sys.columns 
                                    WHERE name = '{col.Name}' AND object_id = OBJECT_ID('[{schemaName}].[{auditTable}]')
                                    """;
                                var maxLength = Convert.ToInt32(lengthCheck.ExecuteScalar() ?? 0);
                                if (maxLength != 100)
                                {
                                    logger.LogInformation("Applying schema patch: correcting column length for {Column} in {Schema}.{Table}.", col.Name, schemaName, auditTable);
                                    using var alter = connection.CreateCommand();
                                    alter.CommandText = $"EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{auditTable}] ALTER COLUMN [{col.Name}] {col.Type}';";
                                    alter.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Comprehensive schema patch for TelecomOperationRequest/AuditLog failed.");
        }
        finally
        {
            if (!wasOpen) connection.Close();
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
                        EXEC sp_executesql N'DROP INDEX IX_MsisdnAsset_Msisdn ON dbo.MsisdnAsset';

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MsisdnAsset') AND name = N'IX_MsisdnAsset_Msisdn_ActiveOnly')
                    BEGIN
                        EXEC sp_executesql N'CREATE UNIQUE NONCLUSTERED INDEX IX_MsisdnAsset_Msisdn_ActiveOnly
                        ON dbo.MsisdnAsset ([Msisdn])
                        WHERE ([IsDeleted] = 0)';
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
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = N'SubscriptionTypeId' AND object_id = OBJECT_ID(N'dbo.{subTable}'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE dbo.[{subTable}] ADD [SubscriptionTypeId] NVARCHAR(50) NULL';
                    END
                    
                    -- Use dynamic SQL to avoid parse errors if column is missing
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE name = N'SubscriptionType' AND object_id = OBJECT_ID(N'dbo.{subTable}'))
                    BEGIN
                        EXEC sp_executesql N'UPDATE dbo.[{subTable}]
                        SET [SubscriptionTypeId] = CASE [SubscriptionType]
                            WHEN 0 THEN N''{prepaid}''
                            WHEN 1 THEN N''{postpaid}''
                            ELSE N''{hybrid}''
                        END
                        WHERE [SubscriptionTypeId] IS NULL';
                    END
                    ELSE
                    BEGIN
                        EXEC sp_executesql N'UPDATE dbo.[{subTable}]
                        SET [SubscriptionTypeId] = N''{prepaid}''
                        WHERE [SubscriptionTypeId] IS NULL';
                    END

                    IF EXISTS (
                        SELECT 1 FROM sys.columns c
                        INNER JOIN sys.tables t ON c.object_id = t.object_id
                        WHERE SCHEMA_NAME(t.schema_id) = N'dbo' AND t.name = N'{subTable}'
                          AND c.name = N'SubscriptionTypeId' AND c.is_nullable = 1)
                    BEGIN
                        EXEC sp_executesql N'UPDATE dbo.[{subTable}] SET [SubscriptionTypeId] = N''{prepaid}'' WHERE [SubscriptionTypeId] IS NULL';
                        EXEC sp_executesql N'ALTER TABLE dbo.[{subTable}] ALTER COLUMN [SubscriptionTypeId] NVARCHAR(50) NOT NULL';
                    END

                    IF NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys fk
                        WHERE fk.parent_object_id = OBJECT_ID(N'dbo.{subTable}')
                          AND fk.referenced_object_id = OBJECT_ID(N'dbo.TelecomSubscriptionTypes'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE dbo.[{subTable}]
                        ADD CONSTRAINT FK_{subTable}_TelecomSubscriptionTypes
                        FOREIGN KEY ([SubscriptionTypeId]) REFERENCES dbo.TelecomSubscriptionTypes ([Id])';
                    END

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE name = N'SubscriptionType' AND object_id = OBJECT_ID(N'dbo.{subTable}'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE dbo.[{subTable}] DROP COLUMN [SubscriptionType]';
                    END

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE name = N'OldSubscriptionType' AND object_id = OBJECT_ID(N'dbo.TelecomMsisdnChangeLog'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE dbo.TelecomMsisdnChangeLog ALTER COLUMN [OldSubscriptionType] NVARCHAR(50) NULL';
                        EXEC sp_executesql N'ALTER TABLE dbo.TelecomMsisdnChangeLog ALTER COLUMN [NewSubscriptionType] NVARCHAR(50) NULL';
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

            var columns = new List<(string Name, string Type)>
            {
                ("EligibilityRules", "NVARCHAR(100) NULL"),
                ("AssetCompatibility", "NVARCHAR(100) NULL"),
                ("BillingCycle", "NVARCHAR(100) NULL"),
                ("TaxCategory", "NVARCHAR(100) NULL"),
                ("ServiceIdSocCode", "NVARCHAR(100) NULL"),
                ("SpeedQuotaLimitGb", "FLOAT NULL"),
                ("VoiceMinutesLimit", "INT NULL"),
                ("ThrottlingPolicy", "NVARCHAR(100) NULL"),
                ("IconClass", "NVARCHAR(100) NULL"),
                ("BadgeColor", "NVARCHAR(100) NULL"),
                ("ShortDescription", "NVARCHAR(500) NULL"),
                ("PaymentType", "INT NULL"),
                ("BillingCycleEnum", "INT NULL")
            };

            foreach (var col in columns)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = N'{col.Name}' AND object_id = OBJECT_ID(N'[{schemaName}].[{tableName}]'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE [{schemaName}].[{tableName}] ADD [{col.Name}] {col.Type}';
                    END
                    """;
                cmd.ExecuteNonQuery();
            }

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

            // 1. ProductOffering.ProductId
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = N'ProductId' AND object_id = OBJECT_ID(N'[{schemaNameOffering}].[{offeringTable}]'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE [{schemaNameOffering}].[{offeringTable}] ADD [ProductId] NVARCHAR(50) NULL';
                    END
                    ELSE
                    BEGIN
                        -- Ensure correct length if already exists
                        IF EXISTS (SELECT 1 FROM sys.columns WHERE name = N'ProductId' AND object_id = OBJECT_ID(N'[{schemaNameOffering}].[{offeringTable}]') AND max_length <> 100)
                        BEGIN
                            EXEC sp_executesql N'ALTER TABLE [{schemaNameOffering}].[{offeringTable}] ALTER COLUMN [ProductId] NVARCHAR(50) NULL';
                        END
                    END
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var fk = connection.CreateCommand())
            {
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

            // 2. TelecomOperationRequest.ProductOfferingId
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = N'ProductOfferingId' AND object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE [{schemaNameOp}].[{opTable}] ADD [ProductOfferingId] NVARCHAR(50) NULL';
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (SELECT 1 FROM sys.columns WHERE name = N'ProductOfferingId' AND object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]') AND max_length <> 100)
                        BEGIN
                            EXEC sp_executesql N'ALTER TABLE [{schemaNameOp}].[{opTable}] ALTER COLUMN [ProductOfferingId] NVARCHAR(50) NULL';
                        END
                    END
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var fk = connection.CreateCommand())
            {
                fk.CommandText = $"""
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
                fk.ExecuteNonQuery();
            }

            // 3. TelecomOperationRequest.PriorProductOfferingId
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = N'PriorProductOfferingId' AND object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE [{schemaNameOp}].[{opTable}] ADD [PriorProductOfferingId] NVARCHAR(50) NULL';
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (SELECT 1 FROM sys.columns WHERE name = N'PriorProductOfferingId' AND object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]') AND max_length <> 100)
                        BEGIN
                            EXEC sp_executesql N'ALTER TABLE [{schemaNameOp}].[{opTable}] ALTER COLUMN [PriorProductOfferingId] NVARCHAR(50) NULL';
                        END
                    END
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var fk = connection.CreateCommand())
            {
                fk.CommandText = $"""
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys
                        WHERE name = N'FK_TelecomOperationRequest_ProductOffering_PriorProductOfferingId'
                          AND parent_object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]'))
                    BEGIN
                        ALTER TABLE [{schemaNameOp}].[{opTable}] WITH CHECK
                        ADD CONSTRAINT [FK_TelecomOperationRequest_ProductOffering_PriorProductOfferingId]
                            FOREIGN KEY ([PriorProductOfferingId]) REFERENCES [{schemaNameOffering}].[{offeringTable}] ([Id]);
                    END
                    """;
                fk.ExecuteNonQuery();
            }

            // 4. TelecomOperationRequest.PriorProductId
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = N'PriorProductId' AND object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]'))
                    BEGIN
                        EXEC sp_executesql N'ALTER TABLE [{schemaNameOp}].[{opTable}] ADD [PriorProductId] NVARCHAR(50) NULL';
                    END
                    ELSE
                    BEGIN
                        IF EXISTS (SELECT 1 FROM sys.columns WHERE name = N'PriorProductId' AND object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]') AND max_length <> 100)
                        BEGIN
                            EXEC sp_executesql N'ALTER TABLE [{schemaNameOp}].[{opTable}] ALTER COLUMN [PriorProductId] NVARCHAR(50) NULL';
                        END
                    END
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var fk = connection.CreateCommand())
            {
                fk.CommandText = $"""
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys
                        WHERE name = N'FK_TelecomOperationRequest_Product_PriorProductId'
                          AND parent_object_id = OBJECT_ID(N'[{schemaNameOp}].[{opTable}]'))
                    BEGIN
                        ALTER TABLE [{schemaNameOp}].[{opTable}] WITH CHECK
                        ADD CONSTRAINT [FK_TelecomOperationRequest_Product_PriorProductId]
                            FOREIGN KEY ([PriorProductId]) REFERENCES [{schemaNameProduct}].[{productTable}] ([Id]);
                    END
                    """;
                fk.ExecuteNonQuery();
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

    private static void ApplyGeoCitySchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        if (RelationalSchemaTableExists(dataContext, "GeoCity"))
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
                IF OBJECT_ID(N'dbo.GeoCity', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.GeoCity (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL CONSTRAINT DF_GeoCity_IsDeleted DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        Name NVARCHAR(255) NULL,
                        Governorate NVARCHAR(255) NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_GeoCity_IsActive DEFAULT 1,
                        SortOrder INT NOT NULL CONSTRAINT DF_GeoCity_SortOrder DEFAULT 0
                    );
                    CREATE INDEX IX_GeoCity_Name_Governorate ON dbo.GeoCity(Name, Governorate);
                    CREATE INDEX IX_GeoCity_Active_Sort ON dbo.GeoCity(IsActive, SortOrder);
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: GeoCity table ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch GeoCity failed.");
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

    private static void ApplyTelecomTechnicalTicketQueueManagementPatch(
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
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TelecomTechnicalTicket' AND COLUMN_NAME = 'AssignedAgentEmail')
                BEGIN
                    ALTER TABLE dbo.TelecomTechnicalTicket ADD AssignedAgentEmail NVARCHAR(128) NULL;
                END
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TelecomTechnicalTicket' AND COLUMN_NAME = 'ClaimedAt')
                BEGIN
                    ALTER TABLE dbo.TelecomTechnicalTicket ADD ClaimedAt DATETIME2 NULL;
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: TelecomTechnicalTicket.AssignedAgentEmail/ClaimedAt ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch TelecomTechnicalTicket queue management columns failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyTelecomTechnicalTicketBranchIdPatch(
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
                    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'TelecomTechnicalTicket' AND COLUMN_NAME = 'BranchId')
                BEGIN
                    ALTER TABLE dbo.TelecomTechnicalTicket ADD BranchId NVARCHAR(50) NULL;
                    CREATE INDEX IX_TelecomTechnicalTicket_BranchId ON dbo.TelecomTechnicalTicket(BranchId) WHERE [BranchId] IS NOT NULL;
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: TelecomTechnicalTicket.BranchId ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch TelecomTechnicalTicket.BranchId failed.");
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

    private static void ApplyIntegrationInfrastructureSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
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
                IF OBJECT_ID(N'dbo.IntegrationOutboxMessage', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.IntegrationOutboxMessage (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        EventType NVARCHAR(256) NOT NULL,
                        PayloadJson NVARCHAR(MAX) NOT NULL,
                        CorrelationId NVARCHAR(128) NULL,
                        OccurredAtUtc DATETIME2 NOT NULL,
                        ProcessedAtUtc DATETIME2 NULL,
                        AttemptCount INT NOT NULL DEFAULT 0,
                        LastError NVARCHAR(MAX) NULL
                    );
                    CREATE INDEX IX_IntegrationOutboxMessage_Processed ON dbo.IntegrationOutboxMessage(ProcessedAtUtc, OccurredAtUtc);
                END

                IF OBJECT_ID(N'dbo.IdempotencyRecord', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.IdempotencyRecord (
                        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                        IsDeleted BIT NOT NULL DEFAULT 0,
                        CreatedAtUtc DATETIME2 NULL,
                        CreatedById NVARCHAR(450) NULL,
                        UpdatedAtUtc DATETIME2 NULL,
                        UpdatedById NVARCHAR(450) NULL,
                        [Key] NVARCHAR(256) NOT NULL,
                        Scope NVARCHAR(128) NOT NULL,
                        RequestHash NVARCHAR(512) NULL,
                        ResponsePayload NVARCHAR(MAX) NULL,
                        ExpiresAtUtc DATETIME2 NOT NULL
                    );
                    CREATE UNIQUE INDEX UX_IdempotencyRecord_Scope_Key ON dbo.IdempotencyRecord(Scope, [Key]) WHERE IsDeleted = 0;
                END
                """;
            cmd.ExecuteNonQuery();
            logger.LogInformation("Schema patch: IntegrationOutboxMessage and IdempotencyRecord ensured.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema patch Integration infrastructure skipped or failed.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void ApplyRowVersionColumnsSchemaPatch(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
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
            // QUOTED_IDENTIFIER required: TelecomOperationRequest / TelecomTechnicalTicket have filtered indexes.
            using (var setCmd = connection.CreateCommand())
            {
                setCmd.CommandText = "SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;";
                setCmd.ExecuteNonQuery();
            }

            string[] tables =
            [
                "MsisdnAsset",
                "SimInventory",
                "TelecomOperationRequest",
                "TelecomTechnicalTicket",
                "DeviceInventory",
            ];

            foreach (var table in tables)
            {
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = """
                    SELECT CASE
                        WHEN OBJECT_ID(N'dbo.' + @table, N'U') IS NOT NULL
                             AND COL_LENGTH(N'dbo.' + @table, N'RowVersion') IS NULL
                        THEN 1 ELSE 0 END
                    """;
                var tableParam = checkCmd.CreateParameter();
                tableParam.ParameterName = "@table";
                tableParam.Value = table;
                checkCmd.Parameters.Add(tableParam);

                var needsColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) == 1;
                if (!needsColumn)
                {
                    continue;
                }

                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE dbo.[{table}] ADD RowVersion rowversion;";
                alterCmd.ExecuteNonQuery();
                logger.LogInformation("Schema patch: added RowVersion to dbo.{Table}.", table);
            }

            logger.LogInformation("Schema patch: RowVersion columns ensured on concurrency-enabled tables.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema patch RowVersion columns failed.");
            throw;
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
    /// Marks pending migrations as applied when the database was created earlier via
    /// <see cref="RelationalDatabaseFacadeExtensions.EnsureCreated"/> (no __EFMigrationsHistory rows).
    /// </summary>
    private static void BaselineLegacyEfMigrationsIfNeeded(DataContext dataContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        if (!dataContext.Database.CanConnect())
        {
            return;
        }

        var applied = dataContext.Database.GetAppliedMigrations().ToList();
        if (applied.Count > 0)
        {
            return;
        }

        var pending = dataContext.Database.GetPendingMigrations().ToList();
        if (pending.Count == 0 || !RelationalSchemaTableExists(dataContext, "AspNetRoles"))
        {
            return;
        }

        var productVersion = ProductInfo.GetVersion();
        var connection = dataContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

        try
        {
            using (var ensureHistoryCmd = connection.CreateCommand())
            {
                ensureHistoryCmd.CommandText = """
                    IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[__EFMigrationsHistory] (
                            [MigrationId] nvarchar(150) NOT NULL,
                            [ProductVersion] nvarchar(32) NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );
                    END
                    """;
                ensureHistoryCmd.ExecuteNonQuery();
            }

            foreach (var migrationId in pending)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = """
                    IF NOT EXISTS (
                        SELECT 1 FROM [dbo].[__EFMigrationsHistory]
                        WHERE [MigrationId] = @migrationId)
                    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES (@migrationId, @productVersion)
                    """;
                var migrationParam = insertCmd.CreateParameter();
                migrationParam.ParameterName = "@migrationId";
                migrationParam.Value = migrationId;
                insertCmd.Parameters.Add(migrationParam);

                var versionParam = insertCmd.CreateParameter();
                versionParam.ParameterName = "@productVersion";
                versionParam.Value = productVersion;
                insertCmd.Parameters.Add(versionParam);

                insertCmd.ExecuteNonQuery();
            }
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }

        logger.LogWarning(
            "Baselined {Count} EF migration(s) on an existing database (legacy EnsureCreated schema). " +
            "Future incremental migrations will still apply normally.",
            pending.Count);
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


