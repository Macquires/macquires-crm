using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations.EfCore;

/// <inheritdoc />
[DbContext(typeof(Contexts.DataContext))]
[Migration("20260615160000_AddTelecomOperationExternalCorrelationId")]
public sealed class AddTelecomOperationExternalCorrelationId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.TelecomOperationRequest', 'ExternalCorrelationId') IS NULL
                ALTER TABLE dbo.TelecomOperationRequest ADD ExternalCorrelationId nvarchar(max) NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ExternalCorrelationId",
            table: "TelecomOperationRequest");
    }
}
