using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class AddImageThumbnails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoyageImageThumbnailPath",
                table: "VoyageImages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VehicleImageThumbnailPath",
                table: "VehicleImages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageThumbnailUrl",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoyageImageThumbnailPath",
                table: "VoyageImages");

            migrationBuilder.DropColumn(
                name: "VehicleImageThumbnailPath",
                table: "VehicleImages");

            migrationBuilder.DropColumn(
                name: "ProfileImageThumbnailUrl",
                table: "AspNetUsers");
        }
    }
}
