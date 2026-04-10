using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class RefineImageThumbnailStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoyageImageThumbnailPath",
                table: "VoyageImages");

            migrationBuilder.DropColumn(
                name: "VehicleImageThumbnailPath",
                table: "VehicleImages");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageThumbnail",
                table: "Voyages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageThumbnailUrl",
                table: "Vehicles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageThumbnail",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "ProfileImageThumbnailUrl",
                table: "Vehicles");

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
        }
    }
}
