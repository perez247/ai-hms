using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EncounterStructuredSchemaAndPatientDemographics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old free-text presenting complaint becomes the structured presenting complaint.
            migrationBuilder.RenameColumn(
                name: "chief_complaint",
                schema: "clinic",
                table: "encounters",
                newName: "presenting_complaint");

            migrationBuilder.AddColumn<string>(
                name: "history_of_presenting_complaint",
                schema: "clinic",
                table: "encounters",
                type: "character varying",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "past_medical_surgical_history",
                schema: "clinic",
                table: "encounters",
                type: "character varying",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_history",
                schema: "clinic",
                table: "encounters",
                type: "character varying",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "examination",
                schema: "clinic",
                table: "encounters",
                type: "character varying",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "disposition",
                schema: "clinic",
                table: "encounters",
                type: "character varying",
                maxLength: 30,
                nullable: true);

            // Legacy free-text notes are preserved as the history of presenting complaint.
            migrationBuilder.Sql("UPDATE \"clinic\".\"encounters\" SET \"history_of_presenting_complaint\" = \"notes\" WHERE \"notes\" IS NOT NULL");
            migrationBuilder.DropColumn(
                name: "notes",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.AddColumn<string>(
                name: "state_of_origin",
                schema: "clinic",
                table: "patients",
                type: "character varying",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "home_town",
                schema: "clinic",
                table: "patients",
                type: "character varying",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "allergy",
                schema: "clinic",
                table: "patients",
                type: "character varying",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "occupation",
                schema: "clinic",
                table: "patients",
                type: "character varying",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "occupation",
                schema: "clinic",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "allergy",
                schema: "clinic",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "home_town",
                schema: "clinic",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "state_of_origin",
                schema: "clinic",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "disposition",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "examination",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "family_history",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "history_of_presenting_complaint",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "past_medical_surgical_history",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.RenameColumn(
                name: "presenting_complaint",
                schema: "clinic",
                table: "encounters",
                newName: "chief_complaint");

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "clinic",
                table: "encounters",
                type: "character varying",
                maxLength: 1000,
                nullable: true);
        }
    }
}
