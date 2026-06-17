using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddTelecomOperationEffectiveDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.TelecomOperationRequest', 'MigrationEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD MigrationEffectiveDateUtc datetime2 NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'ActivationEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD ActivationEffectiveDateUtc datetime2 NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'ReconnectEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD ReconnectEffectiveDateUtc datetime2 NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'RefundEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD RefundEffectiveDateUtc datetime2 NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'BadDebtEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD BadDebtEffectiveDateUtc datetime2 NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'DeviceSaleEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD DeviceSaleEffectiveDateUtc datetime2 NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MigrationEffectiveDateUtc",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "ActivationEffectiveDateUtc",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "ReconnectEffectiveDateUtc",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "RefundEffectiveDateUtc",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "BadDebtEffectiveDateUtc",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "DeviceSaleEffectiveDateUtc",
                table: "TelecomOperationRequest");
        }
    }
}
