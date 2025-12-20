using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class Added_ProfessorInternshipsApplied_StatParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ProfessorInternshipApplied_CandidateCVDownloaded",
                table: "ProfessorInternshipsApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProfessorInternshipApplied_CandidateInfoSeenFromModal",
                table: "ProfessorInternshipsApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfessorInternshipApplied_CandidateCVDownloaded",
                table: "ProfessorInternshipsApplied");

            migrationBuilder.DropColumn(
                name: "ProfessorInternshipApplied_CandidateInfoSeenFromModal",
                table: "ProfessorInternshipsApplied");
        }
    }
}
