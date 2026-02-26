using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class ConversationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReceiverId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConversationKey = table.Column<string>(type: "nvarchar(73)", maxLength: 73, nullable: false),
                    LastMessageId = table.Column<int>(type: "int", nullable: true),
                    LastMessageDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageSenderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastMessagePreview = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_Messages_LastMessageId",
                        column: x => x.LastMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastMessageId",
                table: "Conversations",
                column: "LastMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conversations");
        }
    }
}
