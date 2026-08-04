using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VenueRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDateFlexible = table.Column<bool>(type: "boolean", nullable: false),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    BudgetPerPerson = table.Column<decimal>(type: "numeric", nullable: true),
                    NeedsParking = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReservationId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueRequests_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    HallId = table.Column<int>(type: "integer", nullable: false),
                    MenuId = table.Column<int>(type: "integer", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PricePerPerson = table.Column<decimal>(type: "numeric", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueOffers_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueOffers_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueOffers_VenueRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "VenueRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenueOffers_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VenueOffers_HallId",
                table: "VenueOffers",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueOffers_MenuId",
                table: "VenueOffers",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueOffers_RequestId_VenueId",
                table: "VenueOffers",
                columns: new[] { "RequestId", "VenueId" });

            migrationBuilder.CreateIndex(
                name: "IX_VenueOffers_VenueId",
                table: "VenueOffers",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueRequests_CustomerId",
                table: "VenueRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueRequests_Status_ExpiresAt",
                table: "VenueRequests",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VenueOffers");

            migrationBuilder.DropTable(
                name: "VenueRequests");
        }
    }
}
