using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class alteredConversationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextReceiverEncrypted",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TextSenderEncrypted",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "SenderId",
                table: "Conversations",
                newName: "User2Id");

            migrationBuilder.RenameColumn(
                name: "ReceiverId",
                table: "Conversations",
                newName: "User1Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessageDate",
                table: "Conversations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ConversationKey",
                table: "Conversations",
                type: "nvarchar(73)",
                maxLength: 73,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "User2Id",
                table: "Conversations",
                newName: "SenderId");

            migrationBuilder.RenameColumn(
                name: "User1Id",
                table: "Conversations",
                newName: "ReceiverId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessageDate",
                table: "Conversations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConversationKey",
                table: "Conversations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(73)",
                oldMaxLength: 73);

            migrationBuilder.AddColumn<string>(
                name: "TextReceiverEncrypted",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextSenderEncrypted",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
