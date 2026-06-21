using System.Data;
using Domain.Common;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SeedManager.Demos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataAccessManager.EFCore.SchemaPatches;

/// <summary>
/// Idempotent SQL Server patches for RLS <see cref="IHasBranchId.BranchId"/> columns.
/// Required when legacy EnsureCreated databases were baselined without running InitialCreate.
/// </summary>
public static class RlsBranchIdSchemaPatches
{
    private static readonly (Type EntityType, string ColumnType, (string Name, string Definition)[] Indexes)[] Targets =
    [
        (
            typeof(TelecomOperationRequest),
            "NVARCHAR(50) NULL",
            [("IX_TelecomOperationRequest_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(TelecomOperationAuditLog),
            "NVARCHAR(50) NULL",
            []
        ),
        (
            typeof(DeviceInventory),
            "NVARCHAR(64) NULL",
            [("IX_DeviceInventory_Status_BranchId", "([Status], [BranchId])")]
        ),
        (
            typeof(TelecomTechnicalTicket),
            "NVARCHAR(50) NULL",
            [("IX_TelecomTechnicalTicket_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(Customer),
            "NVARCHAR(50) NULL",
            [("IX_Customer_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(SubscriberProfile),
            "NVARCHAR(50) NULL",
            [("IX_SubscriberProfile_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(MsisdnAsset),
            "NVARCHAR(50) NULL",
            [("IX_MsisdnAsset_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(TelecomPaymentTransaction),
            "NVARCHAR(50) NULL",
            [("IX_TelecomPaymentTransaction_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(TelecomPaymentAuditLog),
            "NVARCHAR(50) NULL",
            []
        ),
        (
            typeof(InventoryBulkImportJob),
            "NVARCHAR(50) NULL",
            [("IX_InventoryBulkImportJob_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(SimInventory),
            "NVARCHAR(50) NULL",
            [("IX_SimInventory_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
        (
            typeof(BillingIntegrationLog),
            "NVARCHAR(50) NULL",
            [("IX_BillingIntegrationLog_BranchId", "([BranchId]) WHERE [BranchId] IS NOT NULL")]
        ),
    ];

    public static void EnsureBranchIdColumns(DataContext dataContext, ILogger logger)
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
            foreach (var (entityType, columnType, indexes) in Targets)
            {
                var mapped = dataContext.Model.FindEntityType(entityType);
                if (mapped is null)
                {
                    continue;
                }

                var tableName = mapped.GetTableName();
                if (string.IsNullOrEmpty(tableName))
                {
                    continue;
                }

                var schemaName = string.IsNullOrEmpty(mapped.GetSchema()) ? "dbo" : mapped.GetSchema()!;

                if (!RelationalSchemaTableExists(connection, schemaName, tableName))
                {
                    continue;
                }

                if (!RelationalSchemaColumnExists(connection, schemaName, tableName, "BranchId"))
                {
                    logger.LogInformation(
                        "Applying schema patch: adding BranchId to {Schema}.{Table}.",
                        schemaName,
                        tableName);

                    using var alter = connection.CreateCommand();
                    alter.CommandText = $"""
                        ALTER TABLE [{schemaName}].[{tableName}]
                        ADD [BranchId] {columnType};
                        """;
                    alter.ExecuteNonQuery();
                }

                foreach (var (indexName, definition) in indexes)
                {
                    if (RelationalSchemaIndexExists(connection, schemaName, tableName, indexName))
                    {
                        continue;
                    }

                    logger.LogInformation(
                        "Applying schema patch: creating index {Index} on {Schema}.{Table}.",
                        indexName,
                        schemaName,
                        tableName);

                    using var index = connection.CreateCommand();
                    index.CommandText = $"""
                        SET QUOTED_IDENTIFIER ON;
                        CREATE INDEX [{indexName}] ON [{schemaName}].[{tableName}] {definition};
                        """;
                    index.ExecuteNonQuery();
                }

                if (!RelationalSchemaColumnExists(connection, schemaName, tableName, "BranchId"))
                {
                    throw new InvalidOperationException(
                        $"Database schema is missing required column [{schemaName}].[{tableName}].[BranchId]. " +
                        "Restart the application after deploying the latest build or run pending EF migrations.");
                }
            }

            logger.LogInformation("Schema patch: RLS BranchId columns ensured.");
            BackfillBranchIdFromOrgUnit(connection, logger);
            DemoSeedScope.ReconcileBranchScopeAsync(dataContext).GetAwaiter().GetResult();
            logger.LogInformation("Schema patch: demo branch scope reconciled.");
        }
        finally
        {
            if (!wasOpen)
            {
                connection.Close();
            }
        }
    }

    private static void BackfillBranchIdFromOrgUnit(IDbConnection connection, ILogger logger)
    {
        logger.LogInformation("Schema patch: backfilling BranchId from OrgUnitId on Customer.");
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.Customer
            SET BranchId = OrgUnitId
            WHERE BranchId IS NULL AND OrgUnitId IS NOT NULL;
            """;
        cmd.ExecuteNonQuery();
    }

    private static bool RelationalSchemaTableExists(IDbConnection connection, string schemaName, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND TABLE_TYPE = 'BASE TABLE'
            """;
        AddParam(cmd, "@schema", schemaName);
        AddParam(cmd, "@table", tableName);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
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
        AddParam(cmd, "@schema", schemaName);
        AddParam(cmd, "@table", tableName);
        AddParam(cmd, "@column", columnName);
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
        AddParam(cmd, "@schema", schemaName);
        AddParam(cmd, "@table", tableName);
        AddParam(cmd, "@index", indexName);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
    }

    private static void AddParam(IDbCommand cmd, string name, string value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
