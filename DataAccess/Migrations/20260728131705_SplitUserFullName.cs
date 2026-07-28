using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SplitUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Users",
                newName: "LastName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Mövcud istifadəçilərin adı itməsin: ilk söz ad, qalan hissə soyad olur
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""FirstName"" = split_part(trim(""LastName""), ' ', 1),
                    ""LastName"" = CASE
                        WHEN position(' ' in trim(""LastName"")) > 0
                        THEN trim(substring(trim(""LastName"") from position(' ' in trim(""LastName"")) + 1))
                        ELSE ''
                    END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ad və soyadı yenidən birləşdirir ki, geri qayıdanda məlumat itməsin
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""LastName"" = trim(""FirstName"" || ' ' || ""LastName"");
            ");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Users",
                newName: "FullName");
        }
    }
}
