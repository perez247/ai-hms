using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PatientProfileAndFitnessWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "flexible_fields_json",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "person_date_of_birth",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.RenameColumn(
                name: "subject_type",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "person_id_number",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "rejection_reason");

            migrationBuilder.RenameColumn(
                name: "paid_at",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "submitted_at");

            migrationBuilder.RenameColumn(
                name: "email",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "certificate_storage_key");

            migrationBuilder.RenameColumn(
                name: "company_name",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "certificate_file_name");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "person_sex",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                schema: "clinic",
                table: "fitness_certificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "certificate_content_type",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "certificate_uploaded_at",
                schema: "clinic",
                table: "fitness_certificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "certificate_uploaded_by",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "patient_age",
                schema: "clinic",
                table: "fitness_certificates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "patient_id",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_updated_at",
                schema: "clinic",
                table: "fitness_certificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_updated_by",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "rejected_at",
                schema: "clinic",
                table: "fitness_certificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rejected_by",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_by",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fitness_investigation_results",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fitness_certificate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fitness_investigation_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_fitness_investigation_results_fitness_certificates_fitness_",
                        column: x => x.fitness_certificate_id,
                        principalSchema: "clinic",
                        principalTable: "fitness_certificates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "patient_files",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_files_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fitness_certificates_patient_id_fitness_date",
                schema: "clinic",
                table: "fitness_certificates",
                columns: new[] { "patient_id", "fitness_date" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_fitness_age",
                schema: "clinic",
                table: "fitness_certificates",
                sql: "patient_age >= 0 AND patient_age <= 130");

            migrationBuilder.CreateIndex(
                name: "ix_fitness_investigation_results_fitness_certificate_id_sort_o",
                schema: "clinic",
                table: "fitness_investigation_results",
                columns: new[] { "fitness_certificate_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_files_patient_id_created_at",
                schema: "clinic",
                table: "patient_files",
                columns: new[] { "patient_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_fitness_certificates_patients_patient_id",
                schema: "clinic",
                table: "fitness_certificates",
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
                name: "fk_fitness_certificates_patients_patient_id",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropTable(
                name: "fitness_investigation_results",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "patient_files",
                schema: "clinic");

            migrationBuilder.DropIndex(
                name: "ix_fitness_certificates_patient_id_fitness_date",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_fitness_age",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "approved_at",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "approved_by",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "certificate_content_type",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "certificate_uploaded_at",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "certificate_uploaded_by",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "patient_age",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "patient_id",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "payment_updated_at",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "payment_updated_by",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "rejected_by",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "submitted_by",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.RenameColumn(
                name: "submitted_at",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "paid_at");

            migrationBuilder.RenameColumn(
                name: "rejection_reason",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "person_id_number");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "subject_type");

            migrationBuilder.RenameColumn(
                name: "certificate_storage_key",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "certificate_file_name",
                schema: "clinic",
                table: "fitness_certificates",
                newName: "company_name");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "person_sex",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "flexible_fields_json",
                schema: "clinic",
                table: "fitness_certificates",
                type: "jsonb",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "person_date_of_birth",
                schema: "clinic",
                table: "fitness_certificates",
                type: "date",
                nullable: true);
        }
    }
}
