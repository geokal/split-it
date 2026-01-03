using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class Added_FKResearchGroupAnnouncements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnnouncementAsResearchGroup_ResearchGroups_ResearchGroupId",
                table: "AnnouncementAsResearchGroup");

            migrationBuilder.DropIndex(
                name: "IX_AnnouncementAsResearchGroup_ResearchGroupId",
                table: "AnnouncementAsResearchGroup");

            migrationBuilder.DropColumn(
                name: "ResearchGroupId",
                table: "AnnouncementAsResearchGroup");

            migrationBuilder.AlterColumn<string>(
                name: "ResearchGroupEmail",
                table: "ResearchGroups",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResearchGroupAnnouncementResearchGroupEmail",
                table: "AnnouncementAsResearchGroup",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResearchGroupAnnouncementRNG_HashedAsUniqueID",
                table: "AnnouncementAsResearchGroup",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ResearchGroups_ResearchGroupEmail",
                table: "ResearchGroups",
                column: "ResearchGroupEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementAsResearchGroup_ResearchGroupAnnouncementResearchGroupEmail",
                table: "AnnouncementAsResearchGroup",
                column: "ResearchGroupAnnouncementResearchGroupEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementAsResearchGroup_ResearchGroupAnnouncementRNG_HashedAsUniqueID",
                table: "AnnouncementAsResearchGroup",
                column: "ResearchGroupAnnouncementRNG_HashedAsUniqueID",
                unique: true,
                filter: "[ResearchGroupAnnouncementRNG_HashedAsUniqueID] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementAsResearchGroup_ResearchGroups_ResearchGroupAnnouncementResearchGroupEmail",
                table: "AnnouncementAsResearchGroup",
                column: "ResearchGroupAnnouncementResearchGroupEmail",
                principalTable: "ResearchGroups",
                principalColumn: "ResearchGroupEmail",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnnouncementAsResearchGroup_ResearchGroups_ResearchGroupAnnouncementResearchGroupEmail",
                table: "AnnouncementAsResearchGroup");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ResearchGroups_ResearchGroupEmail",
                table: "ResearchGroups");

            migrationBuilder.DropIndex(
                name: "IX_AnnouncementAsResearchGroup_ResearchGroupAnnouncementResearchGroupEmail",
                table: "AnnouncementAsResearchGroup");

            migrationBuilder.DropIndex(
                name: "IX_AnnouncementAsResearchGroup_ResearchGroupAnnouncementRNG_HashedAsUniqueID",
                table: "AnnouncementAsResearchGroup");

            migrationBuilder.AlterColumn<string>(
                name: "ResearchGroupEmail",
                table: "ResearchGroups",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ResearchGroupAnnouncementResearchGroupEmail",
                table: "AnnouncementAsResearchGroup",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResearchGroupAnnouncementRNG_HashedAsUniqueID",
                table: "AnnouncementAsResearchGroup",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResearchGroupId",
                table: "AnnouncementAsResearchGroup",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementAsResearchGroup_ResearchGroupId",
                table: "AnnouncementAsResearchGroup",
                column: "ResearchGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementAsResearchGroup_ResearchGroups_ResearchGroupId",
                table: "AnnouncementAsResearchGroup",
                column: "ResearchGroupId",
                principalTable: "ResearchGroups",
                principalColumn: "Id");
        }
    }
}
