using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class VoyageForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Voyages_VoyageId",
                table: "Bids");

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Voyages_VoyageId",
                table: "Bids",
                column: "VoyageId",
                principalTable: "Voyages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bids_Voyages_VoyageId",
                table: "Bids");

            migrationBuilder.AddForeignKey(
                name: "FK_Bids_Voyages_VoyageId",
                table: "Bids",
                column: "VoyageId",
                principalTable: "Voyages",
                principalColumn: "Id");
        }
    }
}
