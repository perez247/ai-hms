using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NurseWorkflowAndLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "encounter_id",
                schema: "clinic",
                table: "vitals",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "patient_id",
                schema: "clinic",
                table: "vitals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing vitals were recorded through encounters; attach them to the patient identity.
            migrationBuilder.Sql("""
                UPDATE clinic.vitals v
                SET patient_id = e.patient_id
                FROM clinic.encounters e
                WHERE v.encounter_id = e.id
                  AND v.patient_id = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.AddColumn<string>(
                name: "reference",
                schema: "clinic",
                table: "account_book_entries",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shift_handovers",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nurse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    content_html = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift_handovers", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_handovers_users_nurse_id",
                        column: x => x.nurse_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vitals_patient_id_recorded_at",
                schema: "clinic",
                table: "vitals",
                columns: new[] { "patient_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_nurse_id",
                schema: "clinic",
                table: "shift_handovers",
                column: "nurse_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_handovers_shift_date_created_at",
                schema: "clinic",
                table: "shift_handovers",
                columns: new[] { "shift_date", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_vitals_patients_patient_id",
                schema: "clinic",
                table: "vitals",
                column: "patient_id",
                principalSchema: "clinic",
                principalTable: "patients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_vitals_patients_patient_id",
                schema: "clinic",
                table: "vitals");

            migrationBuilder.DropTable(
                name: "shift_handovers",
                schema: "clinic");

            migrationBuilder.DropIndex(
                name: "ix_vitals_patient_id_recorded_at",
                schema: "clinic",
                table: "vitals");

            migrationBuilder.DropColumn(
                name: "patient_id",
                schema: "clinic",
                table: "vitals");

            migrationBuilder.DropColumn(
                name: "reference",
                schema: "clinic",
                table: "account_book_entries");

            migrationBuilder.AlterColumn<Guid>(
                name: "encounter_id",
                schema: "clinic",
                table: "vitals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
