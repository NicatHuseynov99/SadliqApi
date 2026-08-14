using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OfferId",
                table: "OfferMessages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "OfferMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfferMessages_ReservationId_CreatedAt",
                table: "OfferMessages",
                columns: new[] { "ReservationId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_OfferMessages_Reservations_ReservationId",
                table: "OfferMessages",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfferMessages_Reservations_ReservationId",
                table: "OfferMessages");

            migrationBuilder.DropIndex(
                name: "IX_OfferMessages_ReservationId_CreatedAt",
                table: "OfferMessages");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "OfferMessages");

            migrationBuilder.AlterColumn<int>(
                name: "OfferId",
                table: "OfferMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
