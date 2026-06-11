using System.Data;
using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataAccessManager.EFCore.SchemaPatches;

/// <summary>
/// Idempotent SQL Server patches for <see cref="MsisdnAsset"/> columns required by IN/CBS routing.
/// </summary>
public static class MsisdnAssetSchemaPatches
{
    private const string IntendedColumn = "IntendedSubscriptionTypeId";
    private const string IntendedIndex = "IX_MsisdnAsset_IntendedSubscriptionTypeId";

    public static void EnsureIntendedSubscriptionTypeColumn(DataContext dataContext, ILogger logger)
    {
        if (!string.Equals(dataContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        var entityType = dataContext.Model.FindEntityType(typeof(MsisdnAsset));
        var tableName = entityType?.GetTableName();
        if (string.IsNullOrEmpty(tableName) || entityType == null)
        {
            return;
        }

        var schemaName = string.IsNullOrEmpty(entityType.GetSchema()) ? "dbo" : entityType.GetSchema()!;

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
            if (!RelationalSchemaColumnExists(connection, schemaName, tableName, IntendedColumn))
            {
                logger.LogInformation(
                    "Applying schema patch: adding {Column} to {Schema}.{Table}.",
                    IntendedColumn,
                    schemaName,
                    tableName);

                using var alter = connection.CreateCommand();
                alter.CommandText = $"""
                    ALTER TABLE [{schemaName}].[{tableName}]
                    ADD [{IntendedColumn}] NVARCHAR(50) NULL;
                    """;
                alter.ExecuteNonQuery();
            }

            if (!RelationalSchemaIndexExists(connection, schemaName, tableName, IntendedIndex))
            {
                logger.LogInformation(
                    "Applying schema patch: creating index {Index} on {Schema}.{Table}.",
                    IntendedIndex,
                    schemaName,
                    tableName);

                using var index = connection.CreateCommand();
                index.CommandText = $"""
                    SET QUOTED_IDENTIFIER ON;
                    CREATE INDEX [{IntendedIndex}] ON [{schemaName}].[{tableName}] ([{IntendedColumn}]);
                    """;
                index.ExecuteNonQuery();
            }

            if (!RelationalSchemaColumnExists(connection, schemaName, tableName, IntendedColumn))
            {
                throw new InvalidOperationException(
                    $"Database schema is missing required column [{schemaName}].[{tableName}].[{IntendedColumn}]. " +
                    "Run Infrastructure/DataAccessManager/EFCore/Migrations/MsisdnAsset_IntendedLineType_Manual.sql " +
                    "or restart after deploying the latest application build.");
            }

            TryBackfillFromProduct(connection, schemaName, tableName, logger);
            TryBackfillFromMsisdnHash(connection, schemaName, tableName, logger);

            logger.LogInformation(
                "Schema patch: {Schema}.{Table}.{Column} ensured.",
                schemaName,
                tableName,
                IntendedColumn);
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void TryBackfillFromProduct(
        IDbConnection connection,
        string schemaName,
        string tableName,
        ILogger logger)
    {
        if (!RelationalSchemaColumnExists(connection, schemaName, "Product", "CompatibleSubscriptionTypeId"))
        {
            return;
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                UPDATE m
                SET m.[{IntendedColumn}] = p.[CompatibleSubscriptionTypeId]
                FROM [{schemaName}].[{tableName}] m
                INNER JOIN [{schemaName}].[Product] p ON p.[Id] = m.[ProductId] AND p.[IsDeleted] = 0
                WHERE m.[IsDeleted] = 0
                  AND m.[{IntendedColumn}] IS NULL
                  AND p.[CompatibleSubscriptionTypeId] IS NOT NULL;
                """;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Backfill {Column} from Product.CompatibleSubscriptionTypeId skipped or failed.", IntendedColumn);
        }
    }

    private static void TryBackfillFromMsisdnHash(
        IDbConnection connection,
        string schemaName,
        string tableName,
        ILogger logger)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                DECLARE @Pre NVARCHAR(50) = N'{TelecomSubscriptionTypeWellKnownIds.Prepaid}';
                DECLARE @Post NVARCHAR(50) = N'{TelecomSubscriptionTypeWellKnownIds.Postpaid}';
                DECLARE @Hyb NVARCHAR(50) = N'{TelecomSubscriptionTypeWellKnownIds.Hybrid}';

                UPDATE m
                SET m.[{IntendedColumn}] = CASE ABS(CHECKSUM(m.[Msisdn])) % 3
                    WHEN 0 THEN @Pre
                    WHEN 1 THEN @Post
                    ELSE @Hyb
                END
                FROM [{schemaName}].[{tableName}] m
                WHERE m.[IsDeleted] = 0 AND m.[{IntendedColumn}] IS NULL;
                """;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Backfill {Column} from MSISDN hash skipped or failed.", IntendedColumn);
        }
    }

    private static bool RelationalSchemaTableExists(DbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            connection.Open();
        }

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
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static bool RelationalSchemaColumnExists(
        IDbConnection connection,
        string schemaName,
        string tableName,
        string columnName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = @column
            """;
        var ps = cmd.CreateParameter();
        ps.ParameterName = "@schema";
        ps.Value = schemaName;
        cmd.Parameters.Add(ps);
        var pt = cmd.CreateParameter();
        pt.ParameterName = "@table";
        pt.Value = tableName;
        cmd.Parameters.Add(pt);
        var pc = cmd.CreateParameter();
        pc.ParameterName = "@column";
        pc.Value = columnName;
        cmd.Parameters.Add(pc);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
    }

    private static bool RelationalSchemaIndexExists(
        IDbConnection connection,
        string schemaName,
        string tableName,
        string indexName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = @schema AND t.name = @table AND i.name = @index
            """;
        var ps = cmd.CreateParameter();
        ps.ParameterName = "@schema";
        ps.Value = schemaName;
        cmd.Parameters.Add(ps);
        var pt = cmd.CreateParameter();
        pt.ParameterName = "@table";
        pt.Value = tableName;
        cmd.Parameters.Add(pt);
        var pi = cmd.CreateParameter();
        pi.ParameterName = "@index";
        pi.Value = indexName;
        cmd.Parameters.Add(pi);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
    }
}
