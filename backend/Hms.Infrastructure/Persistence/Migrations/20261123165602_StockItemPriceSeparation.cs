using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StockItemPriceSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_item_prices_hmo_companies_hmo_company_id",
                schema: "clinic",
                table: "item_prices");

            migrationBuilder.DropForeignKey(
                name: "fk_item_prices_stock_item_stock_item_id",
                schema: "clinic",
                table: "item_prices");

            migrationBuilder.DropTable(
                name: "stock_balances",
                schema: "clinic");

            migrationBuilder.DropPrimaryKey(
                name: "pk_item_prices",
                schema: "clinic",
                table: "item_prices");

            migrationBuilder.DropIndex(
                name: "ix_item_prices_stock_item_id_funding_source_hmo_company_id_eff",
                schema: "clinic",
                table: "item_prices");

            migrationBuilder.RenameTable(
                name: "item_prices",
                schema: "clinic",
                newName: "stock_item_prices",
                newSchema: "clinic");

            migrationBuilder.RenameIndex(
                name: "ix_item_prices_hmo_company_id",
                schema: "clinic",
                table: "stock_item_prices",
                newName: "ix_stock_item_prices_hmo_company_id");

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                schema: "clinic",
                table: "stock_item",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "payer_id",
                schema: "clinic",
                table: "stock_item_prices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "pk_stock_item_prices",
                schema: "clinic",
                table: "stock_item_prices",
                column: "id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_item_quantity",
                schema: "clinic",
                table: "stock_item",
                sql: "quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_stock_item_prices_stock_item_id_payer_id",
                schema: "clinic",
                table: "stock_item_prices",
                columns: new[] { "stock_item_id", "payer_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_item_prices_hmo_companies_hmo_company_id",
                schema: "clinic",
                table: "stock_item_prices",
                column: "hmo_company_id",
                principalSchema: "clinic",
                principalTable: "hmo_companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_item_prices_stock_item_stock_item_id",
                schema: "clinic",
                table: "stock_item_prices",
                column: "stock_item_id",
                principalSchema: "clinic",
                principalTable: "stock_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_item_prices_hmo_companies_hmo_company_id",
                schema: "clinic",
                table: "stock_item_prices");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_item_prices_stock_item_stock_item_id",
                schema: "clinic",
                table: "stock_item_prices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_item_quantity",
                schema: "clinic",
                table: "stock_item");

            migrationBuilder.DropPrimaryKey(
                name: "pk_stock_item_prices",
                schema: "clinic",
                table: "stock_item_prices");

            migrationBuilder.DropIndex(
                name: "ix_stock_item_prices_stock_item_id_payer_id",
                schema: "clinic",
                table: "stock_item_prices");

            migrationBuilder.DropColumn(
                name: "quantity",
                schema: "clinic",
                table: "stock_item");

            migrationBuilder.DropColumn(
                name: "payer_id",
                schema: "clinic",
                table: "stock_item_prices");

            migrationBuilder.RenameTable(
                name: "stock_item_prices",
                schema: "clinic",
                newName: "item_prices",
                newSchema: "clinic");

            migrationBuilder.RenameIndex(
                name: "ix_stock_item_prices_hmo_company_id",
                schema: "clinic",
                table: "item_prices",
                newName: "ix_item_prices_hmo_company_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_item_prices",
                schema: "clinic",
                table: "item_prices",
                column: "id");

            migrationBuilder.CreateTable(
                name: "stock_balances",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    reorder_level = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_balances", x => x.id);
                    table.CheckConstraint("ck_stock_balance", "quantity_on_hand >= 0 AND reorder_level >= 0");
                    table.ForeignKey(
                        name: "fk_stock_balances_stock_item_stock_item_id",
                        column: x => x.stock_item_id,
                        principalSchema: "clinic",
                        principalTable: "stock_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_prices_stock_item_id_funding_source_hmo_company_id_eff",
                schema: "clinic",
                table: "item_prices",
                columns: new[] { "stock_item_id", "funding_source", "hmo_company_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_stock_item_id",
                schema: "clinic",
                table: "stock_balances",
                column: "stock_item_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_item_prices_hmo_companies_hmo_company_id",
                schema: "clinic",
                table: "item_prices",
                column: "hmo_company_id",
                principalSchema: "clinic",
                principalTable: "hmo_companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_item_prices_stock_item_stock_item_id",
                schema: "clinic",
                table: "item_prices",
                column: "stock_item_id",
                principalSchema: "clinic",
                principalTable: "stock_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
