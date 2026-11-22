using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManualBillKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_bill_context",
                schema: "clinic",
                table: "bills");

            migrationBuilder.AddCheckConstraint(
                name: "ck_bill_context",
                schema: "clinic",
                table: "bills",
                sql: "(kind = 'OutpatientEncounter' AND encounter_id IS NOT NULL) OR (kind = 'Emergency' AND encounter_id IS NOT NULL) OR (kind = 'InpatientAdmission' AND admission_id IS NOT NULL) OR (kind = 'Appointment' AND appointment_id IS NOT NULL) OR (kind = 'Registration' AND registration_period_id IS NOT NULL) OR (kind = 'FitnessCertificate' AND fitness_certificate_id IS NOT NULL) OR (kind = 'Manual')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_bill_context",
                schema: "clinic",
                table: "bills");

            migrationBuilder.AddCheckConstraint(
                name: "ck_bill_context",
                schema: "clinic",
                table: "bills",
                sql: "(kind = 'OutpatientEncounter' AND encounter_id IS NOT NULL) OR (kind = 'Emergency' AND encounter_id IS NOT NULL) OR (kind = 'InpatientAdmission' AND admission_id IS NOT NULL) OR (kind = 'Appointment' AND appointment_id IS NOT NULL) OR (kind = 'Registration' AND registration_period_id IS NOT NULL) OR (kind = 'FitnessCertificate' AND fitness_certificate_id IS NOT NULL)");
        }
    }
}
