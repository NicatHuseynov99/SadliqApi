using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanConfirmReservations",
                table: "VenueStaff",
                type: "boolean",
                nullable: false,
                // Mövcud administratorlar əvvəl tam səlahiyyətli idi — davranış
                // dəyişməsin deyə hamısı true ilə açılır. Rəhbər istəsə söndürər.
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanEditCalendar",
                table: "VenueStaff",
                type: "boolean",
                nullable: false,
                // Mövcud administratorlar əvvəl tam səlahiyyətli idi — davranış
                // dəyişməsin deyə hamısı true ilə açılır. Rəhbər istəsə söndürər.
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanSendOffers",
                table: "VenueStaff",
                type: "boolean",
                nullable: false,
                // Mövcud administratorlar əvvəl tam səlahiyyətli idi — davranış
                // dəyişməsin deyə hamısı true ilə açılır. Rəhbər istəsə söndürər.
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanConfirmReservations",
                table: "VenueStaff");

            migrationBuilder.DropColumn(
                name: "CanEditCalendar",
                table: "VenueStaff");

            migrationBuilder.DropColumn(
                name: "CanSendOffers",
                table: "VenueStaff");
        }
    }
}
