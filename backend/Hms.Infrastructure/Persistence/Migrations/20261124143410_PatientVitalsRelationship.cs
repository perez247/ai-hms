using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PatientVitalsRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_vitals_encounters_encounter_id",
                schema: "clinic",
                table: "vitals");

            migrationBuilder.DropIndex(
                name: "ix_vitals_encounter_id_recorded_at",
                schema: "clinic",
                table: "vitals");

            migrationBuilder.DropColumn(
                name: "encounter_id",
                schema: "clinic",
                table: "vitals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "encounter_id",
                schema: "clinic",
                table: "vitals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vitals_encounter_id_recorded_at",
                schema: "clinic",
                table: "vitals",
                columns: new[] { "encounter_id", "recorded_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_vitals_encounters_encounter_id",
                schema: "clinic",
                table: "vitals",
                column: "encounter_id",
                principalSchema: "clinic",
                principalTable: "encounters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
