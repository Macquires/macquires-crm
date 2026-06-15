using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class SyncRlsBranchIdAndMigrationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.TelecomOperationRequest', 'DonorOperatorCode') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD DonorOperatorCode nvarchar(max) NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'NumberChangeEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD NumberChangeEffectiveDateUtc datetime2 NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'PortInMsisdn') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD PortInMsisdn nvarchar(max) NULL;

                IF COL_LENGTH('dbo.TelecomOperationRequest', 'SimSwapEffectiveDateUtc') IS NULL
                    ALTER TABLE dbo.TelecomOperationRequest ADD SimSwapEffectiveDateUtc datetime2 NULL;

                IF COL_LENGTH('dbo.SimInventory', 'BranchId') IS NULL
                    ALTER TABLE dbo.SimInventory ADD BranchId nvarchar(50) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SimInventory_BranchId' AND object_id = OBJECT_ID('dbo.SimInventory'))
                    CREATE INDEX IX_SimInventory_BranchId ON dbo.SimInventory(BranchId) WHERE [BranchId] IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SimInventory_BranchId",
                table: "SimInventory");

            migrationBuilder.DropColumn(
                name: "DonorOperatorCode",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "NumberChangeEffectiveDateUtc",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "PortInMsisdn",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "SimSwapEffectiveDateUtc",
                table: "TelecomOperationRequest");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SimInventory");
        }
    }
}
