using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BillDebtTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "debt_owner",
                schema: "clinic",
                table: "bills",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "debt_reason",
                schema: "clinic",
                table: "bills",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "debt_recorded_at",
                schema: "clinic",
                table: "bills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "debt_recorded_by",
                schema: "clinic",
                table: "bills",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "debt_settled_at",
                schema: "clinic",
                table: "bills",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_debt",
                schema: "clinic",
                table: "bills",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "debt_owner",
                schema: "clinic",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "debt_reason",
                schema: "clinic",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "debt_recorded_at",
                schema: "clinic",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "debt_recorded_by",
                schema: "clinic",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "debt_settled_at",
                schema: "clinic",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "is_debt",
                schema: "clinic",
                table: "bills");
        }
    }
}
