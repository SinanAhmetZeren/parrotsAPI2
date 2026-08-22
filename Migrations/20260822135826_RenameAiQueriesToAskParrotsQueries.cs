using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class RenameAiQueriesToAskParrotsQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AiQueries",
                newName: "AskParrotsQueries");

            migrationBuilder.RenameIndex(
                name: "PK_AiQueries",
                table: "AskParrotsQueries",
                newName: "PK_AskParrotsQueries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AskParrotsQueries",
                newName: "AiQueries");

            migrationBuilder.RenameIndex(
                name: "PK_AskParrotsQueries",
                table: "AiQueries",
                newName: "PK_AiQueries");
        }
    }
}
