using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class waypoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Waypoints");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImage",
                table: "Waypoints",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImage",
                table: "Waypoints");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Waypoints",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
