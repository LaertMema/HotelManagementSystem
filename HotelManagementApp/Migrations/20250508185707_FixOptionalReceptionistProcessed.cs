using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagementApp.Migrations
{
    /// <inheritdoc />
    public partial class FixOptionalReceptionistProcessed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_ProcessedByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProcessedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProcessedBy",
                table: "Payments",
                column: "ProcessedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_ProcessedBy",
                table: "Payments",
                column: "ProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_ProcessedBy",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProcessedBy",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "ProcessedByUserId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProcessedByUserId",
                table: "Payments",
                column: "ProcessedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_ProcessedByUserId",
                table: "Payments",
                column: "ProcessedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
