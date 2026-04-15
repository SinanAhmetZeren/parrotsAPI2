using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsPlaceWithPlaceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPlace",
                table: "Voyages");

            migrationBuilder.AddColumn<int>(
                name: "PlaceType",
                table: "Voyages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaceType",
                table: "Voyages");

            migrationBuilder.AddColumn<bool>(
                name: "IsPlace",
                table: "Voyages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
