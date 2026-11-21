using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FitnessStageAndPatientNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "patient_id",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "patient_number",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_type",
                schema: "clinic",
                table: "fitness_certificates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Normalise the previous fitness lifecycle into the two-stage workflow.
            migrationBuilder.Sql("""
                UPDATE clinic.fitness_certificates
                SET payment_type = CASE WHEN hmo_company_id IS NULL THEN 'Private' ELSE 'Hmo' END,
                    patient_number = p.patient_number
                FROM clinic.patients p
                WHERE fitness_certificates.patient_id = p.id;

                UPDATE clinic.fitness_certificates
                SET payment_type = CASE WHEN hmo_company_id IS NULL THEN 'Private' ELSE 'Hmo' END
                WHERE patient_id IS NULL;

                UPDATE clinic.fitness_certificates SET status = 'AwaitingDoctor'
                WHERE status IN ('Draft', 'PendingDoctorReview', 'Issued');
                UPDATE clinic.fitness_certificates SET status = 'Approved' WHERE status = 'Paid';
                UPDATE clinic.fitness_certificates SET status = 'Rejected' WHERE status = 'Cancelled';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "patient_number",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.DropColumn(
                name: "payment_type",
                schema: "clinic",
                table: "fitness_certificates");

            migrationBuilder.AlterColumn<Guid>(
                name: "patient_id",
                schema: "clinic",
                table: "fitness_certificates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
