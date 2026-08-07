using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferMessageModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HiddenAt",
                table: "OfferMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HiddenByUserId",
                table: "OfferMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HiddenReason",
                table: "OfferMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "OfferMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenAt",
                table: "OfferMessages");

            migrationBuilder.DropColumn(
                name: "HiddenByUserId",
                table: "OfferMessages");

            migrationBuilder.DropColumn(
                name: "HiddenReason",
                table: "OfferMessages");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "OfferMessages");
        }
    }
}
