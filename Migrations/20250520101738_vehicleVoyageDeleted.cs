using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class vehicleVoyageDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Voyages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vehicles");
        }
    }
}
