using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Mig2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketCategories_TicketTypes_TicketTypeId",
                schema: "dbo",
                table: "TicketCategories");

            migrationBuilder.DropIndex(
                name: "IX_TicketCategories_TicketTypeId",
                schema: "dbo",
                table: "TicketCategories");

            migrationBuilder.DropColumn(
                name: "TicketTypeId",
                schema: "dbo",
                table: "TicketCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketTypeId",
                schema: "dbo",
                table: "TicketCategories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCategories_TicketTypeId",
                schema: "dbo",
                table: "TicketCategories",
                column: "TicketTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketCategories_TicketTypes_TicketTypeId",
                schema: "dbo",
                table: "TicketCategories",
                column: "TicketTypeId",
                principalSchema: "dbo",
                principalTable: "TicketTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
