using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class Added_ProfessorThesesApplied_StatParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProfessorThesisApplied_CandidateCVDownloaded",
                table: "ProfessorThesesApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProfessorThesisApplied_CandidateInfoSeenFromModal",
                table: "ProfessorThesesApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfessorThesisApplied_CandidateCVDownloaded",
                table: "ProfessorThesesApplied");

            migrationBuilder.DropColumn(
                name: "ProfessorThesisApplied_CandidateInfoSeenFromModal",
                table: "ProfessorThesesApplied");
        }
    }
}
