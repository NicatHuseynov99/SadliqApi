using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixDisputeUserRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisputeMessages_Users_SenderId",
                table: "DisputeMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_Users_AssignedToId",
                table: "Disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_Users_OpenedById",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_AssignedToId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_OpenedById",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_DisputeMessages_SenderId",
                table: "DisputeMessages");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "OpenedById",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "SenderId",
                table: "DisputeMessages");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_AssignedToUserId",
                table: "Disputes",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_OpenedByUserId",
                table: "Disputes",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_SenderUserId",
                table: "DisputeMessages",
                column: "SenderUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DisputeMessages_Users_SenderUserId",
                table: "DisputeMessages",
                column: "SenderUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_Users_AssignedToUserId",
                table: "Disputes",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_Users_OpenedByUserId",
                table: "Disputes",
                column: "OpenedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisputeMessages_Users_SenderUserId",
                table: "DisputeMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_Users_AssignedToUserId",
                table: "Disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_Disputes_Users_OpenedByUserId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_AssignedToUserId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_Disputes_OpenedByUserId",
                table: "Disputes");

            migrationBuilder.DropIndex(
                name: "IX_DisputeMessages_SenderUserId",
                table: "DisputeMessages");

            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "Disputes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenedById",
                table: "Disputes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SenderId",
                table: "DisputeMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_AssignedToId",
                table: "Disputes",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_OpenedById",
                table: "Disputes",
                column: "OpenedById");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_SenderId",
                table: "DisputeMessages",
                column: "SenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_DisputeMessages_Users_SenderId",
                table: "DisputeMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_Users_AssignedToId",
                table: "Disputes",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Disputes_Users_OpenedById",
                table: "Disputes",
                column: "OpenedById",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
