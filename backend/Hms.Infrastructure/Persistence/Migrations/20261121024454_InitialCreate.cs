using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clinic");

            migrationBuilder.CreateSequence(
                name: "account_entry_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.CreateSequence(
                name: "admission_number_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.CreateSequence(
                name: "appointment_number_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.CreateSequence(
                name: "bill_number_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.CreateSequence(
                name: "certificate_number_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.CreateSequence(
                name: "encounter_number_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.CreateSequence(
                name: "patient_number_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_json = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: true),
                    after_json = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinic_settings",
                schema: "clinic",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    value_json = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clinic_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "fee_configurations",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fee_configurations", x => x.id);
                    table.CheckConstraint("ck_fee_amount", "amount >= 0");
                    table.CheckConstraint("ck_fee_dates", "effective_to IS NULL OR effective_to > effective_from");
                });

            migrationBuilder.CreateTable(
                name: "hmo_companies",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    email = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    phone = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    contract_terms = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hmo_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "investigation_catalog_items",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    tariff_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investigation_catalog_items", x => x.id);
                    table.CheckConstraint("ck_investigation_price", "price >= 0");
                });

            migrationBuilder.CreateTable(
                name: "medications",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    generic_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    form = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    strength = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    unit = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    tariff_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    stock_on_hand = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    reorder_level = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medications", x => x.id);
                    table.CheckConstraint("ck_medication_price", "unit_price >= 0");
                    table.CheckConstraint("ck_medication_stock", "stock_on_hand >= 0");
                });

            migrationBuilder.CreateTable(
                name: "patients",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    first_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    phone = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    email = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    next_of_kin_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    next_of_kin_phone = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    registration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    current_registration_expires_at = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patients", x => x.id);
                    table.CheckConstraint("ck_patient_registration", "current_registration_expires_at > registration_date");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "surgery_catalog_items",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    tariff_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgery_catalog_items", x => x.id);
                    table.CheckConstraint("ck_surgery_price", "price >= 0");
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    staff_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    employee_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    salary_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    bank_account_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    hired_on = table.Column<DateOnly>(type: "date", nullable: true),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deactivation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    security_stamp = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hmo_contracts",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    terms = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    document_ref = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hmo_contracts", x => x.id);
                    table.CheckConstraint("ck_hmo_contract_dates", "end_date > start_date");
                    table.ForeignKey(
                        name: "fk_hmo_contracts_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hmo_tariffs",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_file_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source_file_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    imported_by = table.Column<Guid>(type: "uuid", nullable: true),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hmo_tariffs", x => x.id);
                    table.CheckConstraint("ck_hmo_tariff_dates", "effective_to IS NULL OR effective_to > effective_from");
                    table.ForeignKey(
                        name: "fk_hmo_tariffs_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    medication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    reference_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_medications_medication_id",
                        column: x => x.medication_id,
                        principalSchema: "clinic",
                        principalTable: "medications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_hmo_enrollments",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_member_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    plan_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_hmo_enrollments", x => x.id);
                    table.CheckConstraint("ck_enrollment_dates", "valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "fk_patient_hmo_enrollments_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_hmo_enrollments_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_registration_periods",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    renewal_of_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_registration_periods", x => x.id);
                    table.CheckConstraint("ck_registration_dates", "period_end > period_start");
                    table.CheckConstraint("ck_registration_fee", "fee_amount >= 0");
                    table.ForeignKey(
                        name: "fk_patient_registration_periods_patient_registration_periods_r",
                        column: x => x.renewal_of_id,
                        principalSchema: "clinic",
                        principalTable: "patient_registration_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_patient_registration_periods_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    claim_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "clinic",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointments", x => x.id);
                    table.CheckConstraint("ck_appointment_duration", "duration_minutes > 0");
                    table.ForeignKey(
                        name: "fk_appointments_app_user_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointments_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_ip = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    claim_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "clinic",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    provider_display_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "clinic",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "clinic",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "clinic",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hmo_tariff_items",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_tariff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    unit = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    coverage_percent = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    requires_prior_authorization = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hmo_tariff_items", x => x.id);
                    table.CheckConstraint("ck_tariff_coverage", "coverage_percent >= 0 AND coverage_percent <= 100");
                    table.CheckConstraint("ck_tariff_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "fk_hmo_tariff_items_hmo_tariffs_hmo_tariff_id",
                        column: x => x.hmo_tariff_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_tariffs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admissions",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attending_doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_enrollment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ward_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bed_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    admitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expected_discharge_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    discharged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    discharge_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    discharged_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admissions", x => x.id);
                    table.CheckConstraint("ck_admission_discharge", "discharged_at IS NULL OR discharged_at >= admitted_at");
                    table.ForeignKey(
                        name: "fk_admissions_app_user_attending_doctor_id",
                        column: x => x.attending_doctor_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_patient_hmo_enrollments_hmo_enrollment_id",
                        column: x => x.hmo_enrollment_id,
                        principalSchema: "clinic",
                        principalTable: "patient_hmo_enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_admissions_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fitness_certificates",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    certificate_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    subject_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    person_full_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    person_sex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    person_date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    person_id_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    phone = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    email = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hmo_enrollment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fitness_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    flexible_fields_json = table.Column<string>(type: "jsonb", maxLength: 1000, nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fitness_certificates", x => x.id);
                    table.ForeignKey(
                        name: "fk_fitness_certificates_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fitness_certificates_patient_hmo_enrollments_hmo_enrollment",
                        column: x => x.hmo_enrollment_id,
                        principalSchema: "clinic",
                        principalTable: "patient_hmo_enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bed_days",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    ward_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bed_days", x => x.id);
                    table.CheckConstraint("ck_bed_day_rate", "rate >= 0");
                    table.ForeignKey(
                        name: "fk_bed_days_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "clinic",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "encounters",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_emergency = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hmo_enrollment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    chief_complaint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    diagnosis = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_encounters", x => x.id);
                    table.CheckConstraint("ck_encounter_admission", "(type = 'Outpatient' AND admission_id IS NULL) OR (type = 'Inpatient' AND admission_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_encounters_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "clinic",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_encounters_app_user_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_encounters_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "clinic",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_encounters_patient_hmo_enrollments_hmo_enrollment_id",
                        column: x => x.hmo_enrollment_id,
                        principalSchema: "clinic",
                        principalTable: "patient_hmo_enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_encounters_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investigation_orders",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ordered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    report_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    report_file_ref = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    referral_destination = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investigation_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_investigation_orders_app_user_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_investigation_orders_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_investigation_orders_investigation_catalog_items_catalog_it",
                        column: x => x.catalog_item_id,
                        principalSchema: "clinic",
                        principalTable: "investigation_catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_to_pharmacy_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescriptions_app_user_doctor_id",
                        column: x => x.doctor_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prescriptions_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prior_authorizations",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pa_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: true),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prior_authorizations", x => x.id);
                    table.CheckConstraint("ck_pa_context", "(encounter_id IS NOT NULL AND admission_id IS NULL) OR (encounter_id IS NULL AND admission_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_prior_authorizations_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "clinic",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prior_authorizations_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prior_authorizations_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prior_authorizations_patient_hmo_enrollments_hmo_enrollment",
                        column: x => x.hmo_enrollment_id,
                        principalSchema: "clinic",
                        principalTable: "patient_hmo_enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prior_authorizations_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "surgery_cases",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lead_surgeon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    theater = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    report_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    report_file_ref = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgery_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_surgery_cases_app_user_lead_surgeon_id",
                        column: x => x.lead_surgeon_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_cases_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_cases_surgery_catalog_items_catalog_item_id",
                        column: x => x.catalog_item_id,
                        principalSchema: "clinic",
                        principalTable: "surgery_catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vitals",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nurse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    temperature_c = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    systolic = table.Column<int>(type: "integer", nullable: true),
                    diastolic = table.Column<int>(type: "integer", nullable: true),
                    pulse = table.Column<int>(type: "integer", nullable: true),
                    respiratory_rate = table.Column<int>(type: "integer", nullable: true),
                    spo2 = table.Column<int>(type: "integer", nullable: true),
                    weight_kg = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    height_cm = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    pain_score = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vitals", x => x.id);
                    table.CheckConstraint("ck_vitals_pain", "pain_score IS NULL OR (pain_score >= 0 AND pain_score <= 10)");
                    table.ForeignKey(
                        name: "fk_vitals_app_user_nurse_id",
                        column: x => x.nurse_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_vitals_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "investigation_participants",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investigation_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_role = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_investigation_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_investigation_participants_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_investigation_participants_investigation_orders_investigati",
                        column: x => x.investigation_order_id,
                        principalSchema: "clinic",
                        principalTable: "investigation_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispenses",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pharmacy_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dispensed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tariff_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tariff_confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispenses", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispenses_app_user_pharmacy_user_id",
                        column: x => x.pharmacy_user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dispenses_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dispenses_prescriptions_prescription_id",
                        column: x => x.prescription_id,
                        principalSchema: "clinic",
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prescription_items",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    medication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    route = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    frequency = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    quantity_prescribed = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    quantity_dispensed = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    is_prn = table.Column<bool>(type: "boolean", nullable: false),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescription_items", x => x.id);
                    table.CheckConstraint("ck_prescription_quantity", "quantity_prescribed > 0 AND quantity_dispensed >= 0");
                    table.ForeignKey(
                        name: "fk_prescription_items_medications_medication_id",
                        column: x => x.medication_id,
                        principalSchema: "clinic",
                        principalTable: "medications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prescription_items_prescriptions_prescription_id",
                        column: x => x.prescription_id,
                        principalSchema: "clinic",
                        principalTable: "prescriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bills",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fitness_certificate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hmo_enrollment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prior_authorization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    currency = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    hmo_payable_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    patient_payable_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    paid_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    committed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    committed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    hmo_submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hmo_approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hmo_approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    finance_approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finance_approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bills", x => x.id);
                    table.CheckConstraint("ck_bill_context", "(kind = 'OutpatientEncounter' AND encounter_id IS NOT NULL) OR (kind = 'Emergency' AND encounter_id IS NOT NULL) OR (kind = 'InpatientAdmission' AND admission_id IS NOT NULL) OR (kind = 'Appointment' AND appointment_id IS NOT NULL) OR (kind = 'Registration' AND registration_period_id IS NOT NULL) OR (kind = 'FitnessCertificate' AND fitness_certificate_id IS NOT NULL)");
                    table.CheckConstraint("ck_bill_finance_approval", "status NOT IN ('Approved', 'Paid') OR finance_approved_at IS NOT NULL");
                    table.CheckConstraint("ck_bill_hmo_approval", "status NOT IN ('HmoApproved', 'Approved', 'Paid') OR payer_type <> 'Hmo' OR hmo_approved_at IS NOT NULL");
                    table.CheckConstraint("ck_bill_hmo_context", "payer_type <> 'Hmo' OR hmo_company_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_bills_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "clinic",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalSchema: "clinic",
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_fitness_certificates_fitness_certificate_id",
                        column: x => x.fitness_certificate_id,
                        principalSchema: "clinic",
                        principalTable: "fitness_certificates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_patient_hmo_enrollments_hmo_enrollment_id",
                        column: x => x.hmo_enrollment_id,
                        principalSchema: "clinic",
                        principalTable: "patient_hmo_enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_patient_registration_periods_registration_period_id",
                        column: x => x.registration_period_id,
                        principalSchema: "clinic",
                        principalTable: "patient_registration_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_prior_authorizations_prior_authorization_id",
                        column: x => x.prior_authorization_id,
                        principalSchema: "clinic",
                        principalTable: "prior_authorizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "surgery_participants",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    surgery_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_role = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgery_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_surgery_participants_app_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_surgery_participants_surgery_cases_surgery_case_id",
                        column: x => x.surgery_case_id,
                        principalSchema: "clinic",
                        principalTable: "surgery_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispense_items",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    medication_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    tariff_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hmo_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    patient_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispense_items", x => x.id);
                    table.CheckConstraint("ck_dispense_amounts", "unit_price >= 0 AND hmo_amount >= 0 AND patient_amount >= 0");
                    table.CheckConstraint("ck_dispense_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_dispense_items_dispenses_dispense_id",
                        column: x => x.dispense_id,
                        principalSchema: "clinic",
                        principalTable: "dispenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dispense_items_hmo_tariff_items_tariff_item_id",
                        column: x => x.tariff_item_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_tariff_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dispense_items_medications_medication_id",
                        column: x => x.medication_id,
                        principalSchema: "clinic",
                        principalTable: "medications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dispense_items_prescription_items_prescription_item_id",
                        column: x => x.prescription_item_id,
                        principalSchema: "clinic",
                        principalTable: "prescription_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_adjustments",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_adjustments", x => x.id);
                    table.CheckConstraint("ck_adjustment_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_bill_adjustments_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "clinic",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bill_lines",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    hmo_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    patient_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    is_hmo_covered = table.Column<bool>(type: "boolean", nullable: false),
                    tariff_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tariff_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tariff_confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    hmo_coverage_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hmo_coverage_reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    referral_destination = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    charge_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_lines", x => x.id);
                    table.CheckConstraint("ck_bill_line_amounts", "quantity > 0 AND unit_price >= 0 AND gross_amount >= 0 AND discount_amount >= 0 AND net_amount >= 0 AND hmo_amount >= 0 AND patient_amount >= 0");
                    table.CheckConstraint("ck_bill_line_arithmetic", "net_amount = gross_amount - discount_amount AND net_amount = hmo_amount + patient_amount");
                    table.ForeignKey(
                        name: "fk_bill_lines_admissions_admission_id",
                        column: x => x.admission_id,
                        principalSchema: "clinic",
                        principalTable: "admissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_lines_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "clinic",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bill_lines_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_lines_hmo_tariff_items_tariff_item_id",
                        column: x => x.tariff_item_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_tariff_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_status_histories",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_status_histories_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "clinic",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hmo_receivables",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prior_authorization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pa_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    settled_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hmo_receivables", x => x.id);
                    table.ForeignKey(
                        name: "fk_hmo_receivables_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "clinic",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hmo_receivables_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_hmo_receivables_prior_authorizations_prior_authorization_id",
                        column: x => x.prior_authorization_id,
                        principalSchema: "clinic",
                        principalTable: "prior_authorizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payment_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payments_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "clinic",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "medication_administrations",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prescription_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispense_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nurse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    administered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dose_given = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    route = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    batch_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medication_administrations", x => x.id);
                    table.CheckConstraint("ck_administration_dose", "dose_given > 0");
                    table.ForeignKey(
                        name: "fk_medication_administrations_app_user_nurse_id",
                        column: x => x.nurse_id,
                        principalSchema: "clinic",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_administrations_dispense_items_dispense_item_id",
                        column: x => x.dispense_item_id,
                        principalSchema: "clinic",
                        principalTable: "dispense_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_administrations_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_medication_administrations_prescription_items_prescription_",
                        column: x => x.prescription_item_id,
                        principalSchema: "clinic",
                        principalTable: "prescription_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_book_entries",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    entry_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hmo_receivable_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_book_entries", x => x.id);
                    table.CheckConstraint("ck_account_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_account_book_entries_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "clinic",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_book_entries_hmo_receivables_hmo_receivable_id",
                        column: x => x.hmo_receivable_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_receivables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_book_entries_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "clinic",
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_book_entries_bill_id",
                schema: "clinic",
                table: "account_book_entries",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_book_entries_entry_number",
                schema: "clinic",
                table: "account_book_entries",
                column: "entry_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_book_entries_hmo_receivable_id",
                schema: "clinic",
                table: "account_book_entries",
                column: "hmo_receivable_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_book_entries_occurred_at_entry_type",
                schema: "clinic",
                table: "account_book_entries",
                columns: new[] { "occurred_at", "entry_type" });

            migrationBuilder.CreateIndex(
                name: "ix_account_book_entries_payment_id",
                schema: "clinic",
                table: "account_book_entries",
                column: "payment_id",
                unique: true,
                filter: "payment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_admission_number",
                schema: "clinic",
                table: "admissions",
                column: "admission_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admissions_attending_doctor_id",
                schema: "clinic",
                table: "admissions",
                column: "attending_doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_hmo_enrollment_id",
                schema: "clinic",
                table: "admissions",
                column: "hmo_enrollment_id");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_patient_id",
                schema: "clinic",
                table: "admissions",
                column: "patient_id",
                unique: true,
                filter: "status IN ('Active', 'PendingDischarge')");

            migrationBuilder.CreateIndex(
                name: "ix_admissions_patient_id_admitted_at",
                schema: "clinic",
                table: "admissions",
                columns: new[] { "patient_id", "admitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_appointment_number",
                schema: "clinic",
                table: "appointments",
                column: "appointment_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointments_doctor_id_scheduled_at",
                schema: "clinic",
                table: "appointments",
                columns: new[] { "doctor_id", "scheduled_at" },
                unique: true,
                filter: "status IN ('Scheduled', 'CheckedIn')");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_patient_id_scheduled_at",
                schema: "clinic",
                table: "appointments",
                columns: new[] { "patient_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_user_id_occurred_at",
                schema: "clinic",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_bill_id",
                schema: "clinic",
                table: "audit_logs",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                schema: "clinic",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_occurred_at",
                schema: "clinic",
                table: "audit_logs",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_bed_days_admission_id_service_date",
                schema: "clinic",
                table: "bed_days",
                columns: new[] { "admission_id", "service_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bill_adjustments_bill_id_status",
                schema: "clinic",
                table: "bill_adjustments",
                columns: new[] { "bill_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bill_lines_admission_id",
                schema: "clinic",
                table: "bill_lines",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_lines_bill_id_category",
                schema: "clinic",
                table: "bill_lines",
                columns: new[] { "bill_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_bill_lines_encounter_id",
                schema: "clinic",
                table: "bill_lines",
                column: "encounter_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_lines_source_key",
                schema: "clinic",
                table: "bill_lines",
                column: "source_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bill_lines_tariff_item_id",
                schema: "clinic",
                table: "bill_lines",
                column: "tariff_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_status_histories_bill_id_occurred_at",
                schema: "clinic",
                table: "bill_status_histories",
                columns: new[] { "bill_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bills_admission_id",
                schema: "clinic",
                table: "bills",
                column: "admission_id",
                unique: true,
                filter: "kind = 'InpatientAdmission' AND status <> 'Void'");

            migrationBuilder.CreateIndex(
                name: "ix_bills_appointment_id",
                schema: "clinic",
                table: "bills",
                column: "appointment_id",
                unique: true,
                filter: "kind = 'Appointment' AND status <> 'Void'");

            migrationBuilder.CreateIndex(
                name: "ix_bills_bill_number",
                schema: "clinic",
                table: "bills",
                column: "bill_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bills_encounter_id",
                schema: "clinic",
                table: "bills",
                column: "encounter_id",
                unique: true,
                filter: "kind = 'Emergency' AND status <> 'Void'");

            migrationBuilder.CreateIndex(
                name: "ix_bills_fitness_certificate_id",
                schema: "clinic",
                table: "bills",
                column: "fitness_certificate_id",
                unique: true,
                filter: "kind = 'FitnessCertificate' AND status <> 'Void'");

            migrationBuilder.CreateIndex(
                name: "ix_bills_hmo_company_id_status",
                schema: "clinic",
                table: "bills",
                columns: new[] { "hmo_company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bills_hmo_enrollment_id",
                schema: "clinic",
                table: "bills",
                column: "hmo_enrollment_id");

            migrationBuilder.CreateIndex(
                name: "ix_bills_patient_id_created_at",
                schema: "clinic",
                table: "bills",
                columns: new[] { "patient_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bills_prior_authorization_id",
                schema: "clinic",
                table: "bills",
                column: "prior_authorization_id");

            migrationBuilder.CreateIndex(
                name: "ix_bills_registration_period_id",
                schema: "clinic",
                table: "bills",
                column: "registration_period_id",
                unique: true,
                filter: "kind = 'Registration' AND status <> 'Void'");

            migrationBuilder.CreateIndex(
                name: "ix_bills_status_created_at",
                schema: "clinic",
                table: "bills",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_dispense_items_dispense_id",
                schema: "clinic",
                table: "dispense_items",
                column: "dispense_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispense_items_medication_id",
                schema: "clinic",
                table: "dispense_items",
                column: "medication_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispense_items_prescription_item_id",
                schema: "clinic",
                table: "dispense_items",
                column: "prescription_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispense_items_tariff_item_id",
                schema: "clinic",
                table: "dispense_items",
                column: "tariff_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispenses_encounter_id_created_at",
                schema: "clinic",
                table: "dispenses",
                columns: new[] { "encounter_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_dispenses_pharmacy_user_id",
                schema: "clinic",
                table: "dispenses",
                column: "pharmacy_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispenses_prescription_id",
                schema: "clinic",
                table: "dispenses",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "ix_encounters_admission_id",
                schema: "clinic",
                table: "encounters",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_encounters_appointment_id",
                schema: "clinic",
                table: "encounters",
                column: "appointment_id",
                unique: true,
                filter: "appointment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_encounters_doctor_id_started_at",
                schema: "clinic",
                table: "encounters",
                columns: new[] { "doctor_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_encounters_encounter_number",
                schema: "clinic",
                table: "encounters",
                column: "encounter_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_encounters_hmo_enrollment_id",
                schema: "clinic",
                table: "encounters",
                column: "hmo_enrollment_id");

            migrationBuilder.CreateIndex(
                name: "ix_encounters_patient_id_started_at",
                schema: "clinic",
                table: "encounters",
                columns: new[] { "patient_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fee_configurations_code_effective_from",
                schema: "clinic",
                table: "fee_configurations",
                columns: new[] { "code", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fee_configurations_code_is_active",
                schema: "clinic",
                table: "fee_configurations",
                columns: new[] { "code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_fitness_certificates_certificate_number",
                schema: "clinic",
                table: "fitness_certificates",
                column: "certificate_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fitness_certificates_fitness_date_hmo_company_id_status",
                schema: "clinic",
                table: "fitness_certificates",
                columns: new[] { "fitness_date", "hmo_company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_fitness_certificates_hmo_company_id",
                schema: "clinic",
                table: "fitness_certificates",
                column: "hmo_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_fitness_certificates_hmo_enrollment_id",
                schema: "clinic",
                table: "fitness_certificates",
                column: "hmo_enrollment_id");

            migrationBuilder.CreateIndex(
                name: "ix_hmo_companies_code",
                schema: "clinic",
                table: "hmo_companies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hmo_contracts_end_date",
                schema: "clinic",
                table: "hmo_contracts",
                column: "end_date");

            migrationBuilder.CreateIndex(
                name: "ix_hmo_contracts_hmo_company_id",
                schema: "clinic",
                table: "hmo_contracts",
                column: "hmo_company_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_hmo_contracts_hmo_company_id_status",
                schema: "clinic",
                table: "hmo_contracts",
                columns: new[] { "hmo_company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_hmo_receivables_bill_id",
                schema: "clinic",
                table: "hmo_receivables",
                column: "bill_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hmo_receivables_hmo_company_id",
                schema: "clinic",
                table: "hmo_receivables",
                column: "hmo_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_hmo_receivables_prior_authorization_id",
                schema: "clinic",
                table: "hmo_receivables",
                column: "prior_authorization_id");

            migrationBuilder.CreateIndex(
                name: "ix_hmo_receivables_status_due_date",
                schema: "clinic",
                table: "hmo_receivables",
                columns: new[] { "status", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_hmo_tariff_items_category_code_is_active",
                schema: "clinic",
                table: "hmo_tariff_items",
                columns: new[] { "category", "code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_hmo_tariff_items_hmo_tariff_id_code",
                schema: "clinic",
                table: "hmo_tariff_items",
                columns: new[] { "hmo_tariff_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hmo_tariffs_hmo_company_id",
                schema: "clinic",
                table: "hmo_tariffs",
                column: "hmo_company_id",
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_hmo_tariffs_hmo_company_id_version",
                schema: "clinic",
                table: "hmo_tariffs",
                columns: new[] { "hmo_company_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investigation_catalog_items_code",
                schema: "clinic",
                table: "investigation_catalog_items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investigation_orders_catalog_item_id",
                schema: "clinic",
                table: "investigation_orders",
                column: "catalog_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_investigation_orders_doctor_id",
                schema: "clinic",
                table: "investigation_orders",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_investigation_orders_encounter_id_status",
                schema: "clinic",
                table: "investigation_orders",
                columns: new[] { "encounter_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_investigation_participants_investigation_order_id_user_id_p",
                schema: "clinic",
                table: "investigation_participants",
                columns: new[] { "investigation_order_id", "user_id", "procedure_role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_investigation_participants_user_id",
                schema: "clinic",
                table: "investigation_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_dispense_item_id",
                schema: "clinic",
                table: "medication_administrations",
                column: "dispense_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_encounter_id_administered_at",
                schema: "clinic",
                table: "medication_administrations",
                columns: new[] { "encounter_id", "administered_at" });

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_nurse_id",
                schema: "clinic",
                table: "medication_administrations",
                column: "nurse_id");

            migrationBuilder.CreateIndex(
                name: "ix_medication_administrations_prescription_item_id",
                schema: "clinic",
                table: "medication_administrations",
                column: "prescription_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_medications_code",
                schema: "clinic",
                table: "medications",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_medications_name_is_active",
                schema: "clinic",
                table: "medications",
                columns: new[] { "name", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_hmo_enrollments_hmo_company_id_hmo_member_id",
                schema: "clinic",
                table: "patient_hmo_enrollments",
                columns: new[] { "hmo_company_id", "hmo_member_id" },
                unique: true,
                filter: "is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_patient_hmo_enrollments_patient_id",
                schema: "clinic",
                table: "patient_hmo_enrollments",
                column: "patient_id",
                unique: true,
                filter: "is_primary = TRUE AND is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_patient_registration_periods_patient_id",
                schema: "clinic",
                table: "patient_registration_periods",
                column: "patient_id",
                unique: true,
                filter: "is_current = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_patient_registration_periods_period_end",
                schema: "clinic",
                table: "patient_registration_periods",
                column: "period_end");

            migrationBuilder.CreateIndex(
                name: "ix_patient_registration_periods_renewal_of_id",
                schema: "clinic",
                table: "patient_registration_periods",
                column: "renewal_of_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_current_registration_expires_at",
                schema: "clinic",
                table: "patients",
                column: "current_registration_expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_patients_last_name_first_name",
                schema: "clinic",
                table: "patients",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_patients_patient_number",
                schema: "clinic",
                table: "patients",
                column: "patient_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patients_phone",
                schema: "clinic",
                table: "patients",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_payments_bill_id_received_at",
                schema: "clinic",
                table: "payments",
                columns: new[] { "bill_id", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_idempotency_key",
                schema: "clinic",
                table: "payments",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_items_medication_id",
                schema: "clinic",
                table: "prescription_items",
                column: "medication_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescription_items_prescription_id",
                schema: "clinic",
                table: "prescription_items",
                column: "prescription_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_doctor_id",
                schema: "clinic",
                table: "prescriptions",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_encounter_id_status",
                schema: "clinic",
                table: "prescriptions",
                columns: new[] { "encounter_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_prior_authorizations_admission_id",
                schema: "clinic",
                table: "prior_authorizations",
                column: "admission_id");

            migrationBuilder.CreateIndex(
                name: "ix_prior_authorizations_encounter_id_admission_id",
                schema: "clinic",
                table: "prior_authorizations",
                columns: new[] { "encounter_id", "admission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_prior_authorizations_hmo_company_id_pa_code",
                schema: "clinic",
                table: "prior_authorizations",
                columns: new[] { "hmo_company_id", "pa_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prior_authorizations_hmo_enrollment_id",
                schema: "clinic",
                table: "prior_authorizations",
                column: "hmo_enrollment_id");

            migrationBuilder.CreateIndex(
                name: "ix_prior_authorizations_patient_id",
                schema: "clinic",
                table: "prior_authorizations",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "clinic",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_expires_at",
                schema: "clinic",
                table: "refresh_tokens",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                schema: "clinic",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "clinic",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_medication_id_created_at",
                schema: "clinic",
                table: "stock_movements",
                columns: new[] { "medication_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_surgery_cases_catalog_item_id",
                schema: "clinic",
                table: "surgery_cases",
                column: "catalog_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_cases_encounter_id_status",
                schema: "clinic",
                table: "surgery_cases",
                columns: new[] { "encounter_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_surgery_cases_lead_surgeon_id",
                schema: "clinic",
                table: "surgery_cases",
                column: "lead_surgeon_id");

            migrationBuilder.CreateIndex(
                name: "ix_surgery_catalog_items_code",
                schema: "clinic",
                table: "surgery_catalog_items",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_surgery_participants_surgery_case_id_user_id_procedure_role",
                schema: "clinic",
                table: "surgery_participants",
                columns: new[] { "surgery_case_id", "user_id", "procedure_role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_surgery_participants_user_id",
                schema: "clinic",
                table: "surgery_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                schema: "clinic",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "clinic",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "clinic",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "clinic",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_users_employee_number",
                schema: "clinic",
                table: "users",
                column: "employee_number",
                unique: true,
                filter: "employee_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_staff_role_is_active",
                schema: "clinic",
                table: "users",
                columns: new[] { "staff_role", "is_active" });

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "clinic",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vitals_encounter_id_recorded_at",
                schema: "clinic",
                table: "vitals",
                columns: new[] { "encounter_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_vitals_nurse_id",
                schema: "clinic",
                table: "vitals",
                column: "nurse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_book_entries",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "bed_days",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "bill_adjustments",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "bill_lines",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "bill_status_histories",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "clinic_settings",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "fee_configurations",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "hmo_contracts",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "investigation_participants",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "medication_administrations",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "surgery_participants",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "vitals",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "hmo_receivables",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "investigation_orders",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "dispense_items",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "surgery_cases",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "bills",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "investigation_catalog_items",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "dispenses",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "hmo_tariff_items",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "prescription_items",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "surgery_catalog_items",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "fitness_certificates",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "patient_registration_periods",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "prior_authorizations",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "hmo_tariffs",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "medications",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "prescriptions",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "encounters",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "admissions",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "appointments",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "patient_hmo_enrollments",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "users",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "hmo_companies",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "patients",
                schema: "clinic");

            migrationBuilder.DropSequence(
                name: "account_entry_seq",
                schema: "clinic");

            migrationBuilder.DropSequence(
                name: "admission_number_seq",
                schema: "clinic");

            migrationBuilder.DropSequence(
                name: "appointment_number_seq",
                schema: "clinic");

            migrationBuilder.DropSequence(
                name: "bill_number_seq",
                schema: "clinic");

            migrationBuilder.DropSequence(
                name: "certificate_number_seq",
                schema: "clinic");

            migrationBuilder.DropSequence(
                name: "encounter_number_seq",
                schema: "clinic");

            migrationBuilder.DropSequence(
                name: "patient_number_seq",
                schema: "clinic");
        }
    }
}
