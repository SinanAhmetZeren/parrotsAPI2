using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMessageRenderedAndReadByReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadByReceiver",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Rendered",
                table: "Messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReadByReceiver",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Rendered",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
