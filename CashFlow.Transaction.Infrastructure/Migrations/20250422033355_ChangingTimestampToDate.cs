using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashFlow.Transaction.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangingTimestampToDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "Transactions",
                type: "date", // <-- this is the key change
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "Transactions",
                type: "datetime2", // revert back to original
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");
        }
    }
}