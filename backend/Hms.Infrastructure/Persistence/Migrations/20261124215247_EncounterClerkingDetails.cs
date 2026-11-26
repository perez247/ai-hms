using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EncounterClerkingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "clerking_details",
                schema: "clinic",
                table: "encounters",
                type: "text",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "presenting_complaint_duration_days",
                schema: "clinic",
                table: "encounters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "clerking_details",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "presenting_complaint_duration_days",
                schema: "clinic",
                table: "encounters");
        }
    }
}
