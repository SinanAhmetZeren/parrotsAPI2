using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class ChangeVoyageStateToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing integer values to their string equivalents before changing the column type
            migrationBuilder.Sql(@"
                ALTER TABLE ""Voyages"" ALTER COLUMN ""VoyageState"" TYPE text USING
                    CASE ""VoyageState""
                        WHEN 0 THEN 'Active'
                        WHEN 1 THEN 'BidsClosed'
                        WHEN 2 THEN 'Cancelled'
                        ELSE 'Active'
                    END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VoyageState",
                table: "Voyages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
