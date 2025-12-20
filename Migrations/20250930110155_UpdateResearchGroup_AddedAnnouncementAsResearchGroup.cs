using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class UpdateResearchGroup_AddedAnnouncementAsResearchGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnouncementAsResearchGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchGroupAnnouncementRNG = table.Column<long>(type: "bigint", nullable: true),
                    ResearchGroupAnnouncementRNG_HashedAsUniqueID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroupAnnouncementTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroupAnnouncementDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroupAnnouncementStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroupAnnouncementUploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResearchGroupAnnouncementResearchGroupEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroupAnnouncementTimeToBeActive = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResearchGroupAnnouncementAttachmentFile = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ResearchGroupId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementAsResearchGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnouncementAsResearchGroup_ResearchGroups_ResearchGroupId",
                        column: x => x.ResearchGroupId,
                        principalTable: "ResearchGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementAsResearchGroup_ResearchGroupId",
                table: "AnnouncementAsResearchGroup",
                column: "ResearchGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnouncementAsResearchGroup");
        }
    }
}
