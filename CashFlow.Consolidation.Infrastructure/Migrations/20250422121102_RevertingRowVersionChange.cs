using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.Consolidation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevertingRowVersionChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyBalances_MerchantId_RowVersion",
                table: "DailyBalances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DailyBalances");

            migrationBuilder.CreateIndex(
                name: "IX_DailyBalances_MerchantId_Date",
                table: "DailyBalances",
                columns: new[] { "MerchantId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyBalances_MerchantId_Date",
                table: "DailyBalances");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DailyBalances",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DailyBalances_MerchantId_RowVersion",
                table: "DailyBalances", 
                columns: new[] { "MerchantId", "RowVersion" },
                unique: true);
        }
    }
}