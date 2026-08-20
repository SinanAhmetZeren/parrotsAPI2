using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class AddAiQueryModelRequested : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CoinsAmount",
                table: "CoinPurchases",
                newName: "CrackersAmount");

            migrationBuilder.AddColumn<string>(
                name: "ModelRequested",
                table: "AiQueries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelRequested",
                table: "AiQueries");

            migrationBuilder.RenameColumn(
                name: "CrackersAmount",
                table: "CoinPurchases",
                newName: "CoinsAmount");
        }
    }
}
