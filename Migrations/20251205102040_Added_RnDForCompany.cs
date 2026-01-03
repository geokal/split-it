using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class Added_RnDForCompany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RnD_ContactPersonEmail",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RnD_ContactPersonFullName",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RnD_HeadName",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RnD_ContactPersonEmail",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RnD_ContactPersonFullName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RnD_HeadName",
                table: "Companies");
        }
    }
}
