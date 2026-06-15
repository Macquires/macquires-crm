using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataAccessManager.EFCore.Migrations.EfCore
{
    /// <inheritdoc />
    public partial class AddTelecomTechnicalTicketBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchId",
                table: "TelecomTechnicalTicket",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelecomTechnicalTicket_BranchId",
                table: "TelecomTechnicalTicket",
                column: "BranchId",
                filter: "[BranchId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TelecomTechnicalTicket_BranchId",
                table: "TelecomTechnicalTicket");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "TelecomTechnicalTicket");
        }
    }
}
