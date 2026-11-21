using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CleanArchitectureRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_admissions_app_user_attending_doctor_id",
                schema: "clinic",
                table: "admissions");

            migrationBuilder.DropForeignKey(
                name: "fk_appointments_app_user_doctor_id",
                schema: "clinic",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "fk_dispenses_app_user_pharmacy_user_id",
                schema: "clinic",
                table: "dispenses");

            migrationBuilder.DropForeignKey(
                name: "fk_encounters_app_user_doctor_id",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropForeignKey(
                name: "fk_investigation_orders_app_user_doctor_id",
                schema: "clinic",
                table: "investigation_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_investigation_participants_app_user_user_id",
                schema: "clinic",
                table: "investigation_participants");

            migrationBuilder.DropForeignKey(
                name: "fk_medication_administrations_app_user_nurse_id",
                schema: "clinic",
                table: "medication_administrations");

            migrationBuilder.DropForeignKey(
                name: "fk_prescriptions_app_user_doctor_id",
                schema: "clinic",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_surgery_cases_app_user_lead_surgeon_id",
                schema: "clinic",
                table: "surgery_cases");

            migrationBuilder.DropForeignKey(
                name: "fk_surgery_participants_app_user_user_id",
                schema: "clinic",
                table: "surgery_participants");

            migrationBuilder.DropForeignKey(
                name: "fk_vitals_app_user_nurse_id",
                schema: "clinic",
                table: "vitals");

            migrationBuilder.AddForeignKey(
                name: "fk_admissions_users_attending_doctor_id",
                schema: "clinic",
                table: "admissions",
                column: "attending_doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_users_doctor_id",
                schema: "clinic",
                table: "appointments",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dispenses_users_pharmacy_user_id",
                schema: "clinic",
                table: "dispenses",
                column: "pharmacy_user_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_encounters_users_doctor_id",
                schema: "clinic",
                table: "encounters",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_investigation_orders_users_doctor_id",
                schema: "clinic",
                table: "investigation_orders",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_investigation_participants_users_user_id",
                schema: "clinic",
                table: "investigation_participants",
                column: "user_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_medication_administrations_users_nurse_id",
                schema: "clinic",
                table: "medication_administrations",
                column: "nurse_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_prescriptions_users_doctor_id",
                schema: "clinic",
                table: "prescriptions",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_surgery_cases_users_lead_surgeon_id",
                schema: "clinic",
                table: "surgery_cases",
                column: "lead_surgeon_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_surgery_participants_users_user_id",
                schema: "clinic",
                table: "surgery_participants",
                column: "user_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vitals_users_nurse_id",
                schema: "clinic",
                table: "vitals",
                column: "nurse_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_admissions_users_attending_doctor_id",
                schema: "clinic",
                table: "admissions");

            migrationBuilder.DropForeignKey(
                name: "fk_appointments_users_doctor_id",
                schema: "clinic",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "fk_dispenses_users_pharmacy_user_id",
                schema: "clinic",
                table: "dispenses");

            migrationBuilder.DropForeignKey(
                name: "fk_encounters_users_doctor_id",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropForeignKey(
                name: "fk_investigation_orders_users_doctor_id",
                schema: "clinic",
                table: "investigation_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_investigation_participants_users_user_id",
                schema: "clinic",
                table: "investigation_participants");

            migrationBuilder.DropForeignKey(
                name: "fk_medication_administrations_users_nurse_id",
                schema: "clinic",
                table: "medication_administrations");

            migrationBuilder.DropForeignKey(
                name: "fk_prescriptions_users_doctor_id",
                schema: "clinic",
                table: "prescriptions");

            migrationBuilder.DropForeignKey(
                name: "fk_surgery_cases_users_lead_surgeon_id",
                schema: "clinic",
                table: "surgery_cases");

            migrationBuilder.DropForeignKey(
                name: "fk_surgery_participants_users_user_id",
                schema: "clinic",
                table: "surgery_participants");

            migrationBuilder.DropForeignKey(
                name: "fk_vitals_users_nurse_id",
                schema: "clinic",
                table: "vitals");

            migrationBuilder.AddForeignKey(
                name: "fk_admissions_app_user_attending_doctor_id",
                schema: "clinic",
                table: "admissions",
                column: "attending_doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_app_user_doctor_id",
                schema: "clinic",
                table: "appointments",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_dispenses_app_user_pharmacy_user_id",
                schema: "clinic",
                table: "dispenses",
                column: "pharmacy_user_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_encounters_app_user_doctor_id",
                schema: "clinic",
                table: "encounters",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_investigation_orders_app_user_doctor_id",
                schema: "clinic",
                table: "investigation_orders",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_investigation_participants_app_user_user_id",
                schema: "clinic",
                table: "investigation_participants",
                column: "user_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_medication_administrations_app_user_nurse_id",
                schema: "clinic",
                table: "medication_administrations",
                column: "nurse_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_prescriptions_app_user_doctor_id",
                schema: "clinic",
                table: "prescriptions",
                column: "doctor_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_surgery_cases_app_user_lead_surgeon_id",
                schema: "clinic",
                table: "surgery_cases",
                column: "lead_surgeon_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_surgery_participants_app_user_user_id",
                schema: "clinic",
                table: "surgery_participants",
                column: "user_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_vitals_app_user_nurse_id",
                schema: "clinic",
                table: "vitals",
                column: "nurse_id",
                principalSchema: "clinic",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
