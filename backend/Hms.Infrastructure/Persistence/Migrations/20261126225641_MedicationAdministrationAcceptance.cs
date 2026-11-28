using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MedicationAdministrationAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "accepted",
                schema: "clinic",
                table: "medication_administrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "non_acceptance_reason",
                schema: "clinic",
                table: "medication_administrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nurse_name",
                schema: "clinic",
                table: "medication_administrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accepted",
                schema: "clinic",
                table: "medication_administrations");

            migrationBuilder.DropColumn(
                name: "non_acceptance_reason",
                schema: "clinic",
                table: "medication_administrations");

            migrationBuilder.DropColumn(
                name: "nurse_name",
                schema: "clinic",
                table: "medication_administrations");
        }
    }
}
