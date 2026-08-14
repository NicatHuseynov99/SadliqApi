using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationRespondedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RespondedByUserId",
                table: "Reservations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RespondedByUserId",
                table: "Reservations",
                column: "RespondedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_RespondedByUserId",
                table: "Reservations",
                column: "RespondedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_RespondedByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RespondedByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RespondedByUserId",
                table: "Reservations");
        }
    }
}
