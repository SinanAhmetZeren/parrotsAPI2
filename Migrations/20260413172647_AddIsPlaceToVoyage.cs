using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPlaceToVoyage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPlace",
                table: "Voyages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPlace",
                table: "Voyages");
        }
    }
}
