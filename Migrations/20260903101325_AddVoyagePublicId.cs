using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class AddVoyagePublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Voyages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    v RECORD;
                    new_id TEXT;
                    chars TEXT := 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
                BEGIN
                    FOR v IN SELECT ""Id"" FROM ""Voyages"" LOOP
                        LOOP
                            new_id := '';
                            FOR i IN 1..10 LOOP
                                new_id := new_id || substr(chars, floor(random() * 62 + 1)::int, 1);
                            END LOOP;
                            EXIT WHEN NOT EXISTS (SELECT 1 FROM ""Voyages"" WHERE ""PublicId"" = new_id);
                        END LOOP;
                        UPDATE ""Voyages"" SET ""PublicId"" = new_id WHERE ""Id"" = v.""Id"";
                    END LOOP;
                END $$;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Voyages_PublicId",
                table: "Voyages",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voyages_PublicId",
                table: "Voyages");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Voyages");
        }
    }
}
