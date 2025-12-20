using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class Added_Company_Jobs_Thesis_Internships_StatParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CompanyInternshipApplied_CandidateCVDownloaded",
                table: "InternshipsApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyInternshipApplied_CandidateInfoSeenFromModal",
                table: "InternshipsApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyThesisApplied_CandidateCVDownloaded",
                table: "CompanyThesesApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyThesisApplied_CandidateInfoSeenFromModal",
                table: "CompanyThesesApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyJobApplied_CandidateCVDownloaded",
                table: "CompanyJobsApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyJobApplied_CandidateInfoSeenFromModal",
                table: "CompanyJobsApplied",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyInternshipApplied_CandidateCVDownloaded",
                table: "InternshipsApplied");

            migrationBuilder.DropColumn(
                name: "CompanyInternshipApplied_CandidateInfoSeenFromModal",
                table: "InternshipsApplied");

            migrationBuilder.DropColumn(
                name: "CompanyThesisApplied_CandidateCVDownloaded",
                table: "CompanyThesesApplied");

            migrationBuilder.DropColumn(
                name: "CompanyThesisApplied_CandidateInfoSeenFromModal",
                table: "CompanyThesesApplied");

            migrationBuilder.DropColumn(
                name: "CompanyJobApplied_CandidateCVDownloaded",
                table: "CompanyJobsApplied");

            migrationBuilder.DropColumn(
                name: "CompanyJobApplied_CandidateInfoSeenFromModal",
                table: "CompanyJobsApplied");
        }
    }
}
