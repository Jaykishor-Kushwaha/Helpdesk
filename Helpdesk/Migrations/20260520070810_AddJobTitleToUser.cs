using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helpdesk.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTitleToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "AspNetUsers");
        }
    }
}
