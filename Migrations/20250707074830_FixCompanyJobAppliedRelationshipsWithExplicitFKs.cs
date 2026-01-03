using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class FixCompanyJobAppliedRelationshipsWithExplicitFKs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyJobApplied_CompanyDetails_CompanyJobsApplied_CompanysEmailWhereStudentAppliedForCompanyJob",
                table: "CompanyJobApplied_CompanyDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyJobApplied_StudentDetails_CompanyJobsApplied_StudentEmailAppliedForCompanyJob",
                table: "CompanyJobApplied_StudentDetails");

            // Drop existing unique constraints
            migrationBuilder.DropUniqueConstraint(
                name: "AK_CompanyJobsApplied_CompanysEmailWhereStudentAppliedForCompanyJob",
                table: "CompanyJobsApplied");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CompanyJobsApplied_StudentEmailAppliedForCompanyJob",
                table: "CompanyJobsApplied");

            // Add explicit foreign key columns to detail tables
            migrationBuilder.AddColumn<int>(
                name: "CompanyJobAppliedId",
                table: "CompanyJobApplied_CompanyDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyJobAppliedId",
                table: "CompanyJobApplied_StudentDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Create foreign keys with explicit FK columns (not identity columns)
            migrationBuilder.AddForeignKey(
                name: "FK_CompanyJobApplied_CompanyDetails_CompanyJobAppliedId",
                table: "CompanyJobApplied_CompanyDetails",
                column: "CompanyJobAppliedId",
                principalTable: "CompanyJobsApplied",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyJobApplied_StudentDetails_CompanyJobAppliedId",
                table: "CompanyJobApplied_StudentDetails",
                column: "CompanyJobAppliedId",
                principalTable: "CompanyJobsApplied",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Add composite unique constraint
            migrationBuilder.CreateIndex(
                name: "IX_CompanyJobsApplied_StudentEmail_RNG",
                table: "CompanyJobsApplied",
                columns: new[] { "StudentEmailAppliedForCompanyJob", "RNGForCompanyJobApplied" },
                unique: true,
                filter: "[StudentEmailAppliedForCompanyJob] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyJobsApplied_StudentEmail_RNG",
                table: "CompanyJobsApplied");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyJobApplied_CompanyDetails_CompanyJobAppliedId",
                table: "CompanyJobApplied_CompanyDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyJobApplied_StudentDetails_CompanyJobAppliedId",
                table: "CompanyJobApplied_StudentDetails");

            migrationBuilder.DropColumn(
                name: "CompanyJobAppliedId",
                table: "CompanyJobApplied_CompanyDetails");

            migrationBuilder.DropColumn(
                name: "CompanyJobAppliedId",
                table: "CompanyJobApplied_StudentDetails");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CompanyJobsApplied_CompanysEmailWhereStudentAppliedForCompanyJob",
                table: "CompanyJobsApplied",
                column: "CompanysEmailWhereStudentAppliedForCompanyJob");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CompanyJobsApplied_StudentEmailAppliedForCompanyJob",
                table: "CompanyJobsApplied",
                column: "StudentEmailAppliedForCompanyJob");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyJobApplied_CompanyDetails_CompanyJobsApplied_CompanysEmailWhereStudentAppliedForCompanyJob",
                table: "CompanyJobApplied_CompanyDetails",
                column: "CompanysEmailWhereStudentAppliedForCompanyJob",
                principalTable: "CompanyJobsApplied",
                principalColumn: "CompanysEmailWhereStudentAppliedForCompanyJob",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyJobApplied_StudentDetails_CompanyJobsApplied_StudentEmailAppliedForCompanyJob",
                table: "CompanyJobApplied_StudentDetails",
                column: "StudentEmailAppliedForCompanyJob",
                principalTable: "CompanyJobsApplied",
                principalColumn: "StudentEmailAppliedForCompanyJob",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
