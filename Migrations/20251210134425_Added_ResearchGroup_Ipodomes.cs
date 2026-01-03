using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizManager.Migrations
{
    public partial class Added_ResearchGroup_Ipodomes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResearchGroup_Ipodomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchGroupEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroup_UniqueID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroup_Ipodomes_Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroup_Ipodomes_Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroup_Ipodomes_Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchGroup_Ipodomes_Attachment = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ResearchGroup_Ipodomes_Images = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchGroup_Ipodomes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchGroup_Ipodomes");
        }
    }
}
