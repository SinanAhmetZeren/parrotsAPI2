using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class AddDualEncryptedTextToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Text",
                table: "Messages",
                newName: "TextSenderEncrypted");

            migrationBuilder.AddColumn<string>(
                name: "TextReceiverEncrypted",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextReceiverEncrypted",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "TextSenderEncrypted",
                table: "Messages",
                newName: "Text");
        }
    }
}
