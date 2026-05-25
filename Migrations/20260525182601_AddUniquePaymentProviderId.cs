using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotsAPI2.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePaymentProviderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CoinPurchases_PaymentProviderId",
                table: "CoinPurchases",
                column: "PaymentProviderId",
                unique: true,
                filter: "\"PaymentProviderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CoinPurchases_PaymentProviderId",
                table: "CoinPurchases");
        }
    }
}
