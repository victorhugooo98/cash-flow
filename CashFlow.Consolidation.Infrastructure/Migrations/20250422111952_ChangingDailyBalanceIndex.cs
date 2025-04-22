using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.Consolidation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangingDailyBalanceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyBalances_MerchantId_Date",
                table: "DailyBalances");

            migrationBuilder.CreateIndex(
                name: "IX_DailyBalances_MerchantId_RowVersion",
                table: "DailyBalances",
                columns: new[] { "MerchantId", "RowVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyBalances_MerchantId_RowVersion",
                table: "DailyBalances");

            migrationBuilder.CreateIndex(
                name: "IX_DailyBalances_MerchantId_Date",
                table: "DailyBalances",
                columns: new[] { "MerchantId", "Date" },
                unique: true);
        }
    }
}
