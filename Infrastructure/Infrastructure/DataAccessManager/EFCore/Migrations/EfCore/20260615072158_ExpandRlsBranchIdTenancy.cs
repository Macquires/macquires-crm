using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class ExpandRlsBranchIdTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Batch 1: add columns only (SQL Server validates CREATE INDEX at compile time).
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.TelecomPaymentTransaction', 'BranchId') IS NULL
                    ALTER TABLE dbo.TelecomPaymentTransaction ADD BranchId nvarchar(50) NULL;

                IF COL_LENGTH('dbo.TelecomPaymentAuditLog', 'BranchId') IS NULL
                    ALTER TABLE dbo.TelecomPaymentAuditLog ADD BranchId nvarchar(50) NULL;

                IF COL_LENGTH('dbo.SubscriberProfile', 'BranchId') IS NULL
                    ALTER TABLE dbo.SubscriberProfile ADD BranchId nvarchar(50) NULL;

                IF COL_LENGTH('dbo.MsisdnAsset', 'BranchId') IS NULL
                    ALTER TABLE dbo.MsisdnAsset ADD BranchId nvarchar(50) NULL;

                IF COL_LENGTH('dbo.Customer', 'BranchId') IS NULL
                    ALTER TABLE dbo.Customer ADD BranchId nvarchar(50) NULL;
                """);

            // Batch 2: indexes after columns exist.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TelecomPaymentTransaction_BranchId' AND object_id = OBJECT_ID('dbo.TelecomPaymentTransaction'))
                    CREATE INDEX IX_TelecomPaymentTransaction_BranchId ON dbo.TelecomPaymentTransaction(BranchId) WHERE [BranchId] IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SubscriberProfile_BranchId' AND object_id = OBJECT_ID('dbo.SubscriberProfile'))
                    CREATE INDEX IX_SubscriberProfile_BranchId ON dbo.SubscriberProfile(BranchId) WHERE [BranchId] IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MsisdnAsset_BranchId' AND object_id = OBJECT_ID('dbo.MsisdnAsset'))
                    CREATE INDEX IX_MsisdnAsset_BranchId ON dbo.MsisdnAsset(BranchId) WHERE [BranchId] IS NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customer_BranchId' AND object_id = OBJECT_ID('dbo.Customer'))
                    CREATE INDEX IX_Customer_BranchId ON dbo.Customer(BranchId) WHERE [BranchId] IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelecomPaymentTransaction_BranchId",
                table: "TelecomPaymentTransaction");

            migrationBuilder.DropIndex(
                name: "IX_SubscriberProfile_BranchId",
                table: "SubscriberProfile");

            migrationBuilder.DropIndex(
                name: "IX_MsisdnAsset_BranchId",
                table: "MsisdnAsset");

            migrationBuilder.DropIndex(
                name: "IX_Customer_BranchId",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "TelecomPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "TelecomPaymentAuditLog");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "SubscriberProfile");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "MsisdnAsset");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Customer");
        }
    }
}
