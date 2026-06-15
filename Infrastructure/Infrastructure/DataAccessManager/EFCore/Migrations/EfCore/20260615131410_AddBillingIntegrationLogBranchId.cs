using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddBillingIntegrationLogBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.BillingIntegrationLog', 'BranchId') IS NULL
                    ALTER TABLE dbo.BillingIntegrationLog ADD BranchId nvarchar(50) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BillingIntegrationLog_BranchId' AND object_id = OBJECT_ID('dbo.BillingIntegrationLog'))
                    CREATE INDEX IX_BillingIntegrationLog_BranchId ON dbo.BillingIntegrationLog(BranchId) WHERE [BranchId] IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingIntegrationLog_BranchId",
                table: "BillingIntegrationLog");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BillingIntegrationLog");
        }
    }
}
