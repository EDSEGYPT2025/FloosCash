using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloosCash.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletLimitsAndCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DefaultCommissionRate",
                table: "Wallets",
                newName: "WithdrawalCommissionRate");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDepositLimit",
                table: "Wallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyWithdrawalLimit",
                table: "Wallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositCommissionRate",
                table: "Wallets",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyDepositLimit",
                table: "Wallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyWithdrawalLimit",
                table: "Wallets",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyDepositLimit",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "DailyWithdrawalLimit",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "DepositCommissionRate",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "MonthlyDepositLimit",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "MonthlyWithdrawalLimit",
                table: "Wallets");

            migrationBuilder.RenameColumn(
                name: "WithdrawalCommissionRate",
                table: "Wallets",
                newName: "DefaultCommissionRate");
        }
    }
}
