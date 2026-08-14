using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class RenameParrotCoinBalanceToParrotCrackerBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParrotCoinBalance",
                table: "AspNetUsers",
                newName: "ParrotCrackerBalance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParrotCrackerBalance",
                table: "AspNetUsers",
                newName: "ParrotCoinBalance");
        }
    }
}
