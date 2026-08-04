using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationSeenBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeenByUserId",
                table: "Reservations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_SeenByUserId",
                table: "Reservations",
                column: "SeenByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_SeenByUserId",
                table: "Reservations",
                column: "SeenByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_SeenByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_SeenByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SeenByUserId",
                table: "Reservations");
        }
    }
}
