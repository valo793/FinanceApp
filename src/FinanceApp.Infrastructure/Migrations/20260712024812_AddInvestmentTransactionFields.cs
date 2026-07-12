using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentTransactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvestmentId",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InvestmentQuantity",
                table: "transactions",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "transactions",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_InvestmentId",
                table: "transactions",
                column: "InvestmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_InvestmentId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "InvestmentId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "InvestmentQuantity",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "transactions");
        }
    }
}
