using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedGuestReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerifiedGuest",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "Reviews",
                type: "integer",
                nullable: true);

            // Bu qayda qüvvəyə minənədək yazılmış rəylər təsdiqlənmiş sayılır —
            // yoxsa mövcud sarayların reytinqi bir gecədə sıfırlanardı.
            migrationBuilder.Sql(@"UPDATE ""Reviews"" SET ""IsVerifiedGuest"" = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerifiedGuest",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "Reviews");
        }
    }
}
