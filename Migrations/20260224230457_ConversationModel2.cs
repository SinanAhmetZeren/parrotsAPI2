using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class ConversationModel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Messages_LastMessageId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_LastMessageId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "LastMessagePreview",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "LastMessageSenderId",
                table: "Conversations");

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "ConversationKey",
                table: "Conversations",
                type: "nvarchar(max)",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextReceiverEncrypted",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TextSenderEncrypted",
                table: "Conversations");

            migrationBuilder.AlterColumn<string>(
                name: "SenderId",
                table: "Conversations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverId",
                table: "Conversations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ConversationKey",
                table: "Conversations",
                type: "nvarchar(73)",
                maxLength: 73,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "LastMessagePreview",
                table: "Conversations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessageSenderId",
                table: "Conversations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageId",
                table: "Conversations",
                column: "LastMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Messages_LastMessageId",
                table: "Conversations",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id");
        }
    }
}
