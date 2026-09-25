using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class AddVoyageAdviceColumnsToAskParrotsQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QueryType",
                table: "AskParrotsQueries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoyageAdviceRequestJson",
                table: "AskParrotsQueries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoyageAdviceResponse",
                table: "AskParrotsQueries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QueryType",
                table: "AskParrotsQueries");

            migrationBuilder.DropColumn(
                name: "VoyageAdviceRequestJson",
                table: "AskParrotsQueries");

            migrationBuilder.DropColumn(
                name: "VoyageAdviceResponse",
                table: "AskParrotsQueries");
        }
    }
}
