using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FundingStockAndServiceResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dispense_items_medications_medication_id",
                schema: "clinic",
                table: "dispense_items");

            migrationBuilder.DropForeignKey(
                name: "fk_investigation_orders_investigation_catalog_items_catalog_it",
                schema: "clinic",
                table: "investigation_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_prescription_items_medications_medication_id",
                schema: "clinic",
                table: "prescription_items");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_medications_medication_id",
                schema: "clinic",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_surgery_cases_surgery_catalog_items_catalog_item_id",
                schema: "clinic",
                table: "surgery_cases");

            migrationBuilder.DropTable(
                name: "investigation_catalog_items",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "medications",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "surgery_catalog_items",
                schema: "clinic");

            migrationBuilder.RenameColumn(
                name: "medication_id",
                schema: "clinic",
                table: "stock_movements",
                newName: "stock_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_stock_movements_medication_id_created_at",
                schema: "clinic",
                table: "stock_movements",
                newName: "ix_stock_movements_stock_item_id_created_at");

            migrationBuilder.RenameColumn(
                name: "medication_id",
                schema: "clinic",
                table: "prescription_items",
                newName: "stock_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_prescription_items_medication_id",
                schema: "clinic",
                table: "prescription_items",
                newName: "ix_prescription_items_stock_item_id");

            migrationBuilder.RenameColumn(
                name: "medication_id",
                schema: "clinic",
                table: "dispense_items",
                newName: "stock_item_id");

            migrationBuilder.RenameIndex(
                name: "ix_dispense_items_medication_id",
                schema: "clinic",
                table: "dispense_items",
                newName: "ix_dispense_items_stock_item_id");

            migrationBuilder.CreateSequence(
                name: "service_request_number_seq",
                schema: "clinic",
                startValue: 1000L);

            migrationBuilder.AddColumn<string>(
                name: "funding_source",
                schema: "clinic",
                table: "encounters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "funding_source",
                schema: "clinic",
                table: "appointments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "hmo_enrollment_id",
                schema: "clinic",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stock_item",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    item_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    generic_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    form = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    strength = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    unit = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    tariff_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_item", x => x.id);
                    table.CheckConstraint("ck_stock_item_code", "code <> ''");
                });

            migrationBuilder.CreateTable(
                name: "item_prices",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    funding_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    coverage_percent = table.Column<int>(type: "integer", nullable: false),
                    requires_prior_authorization = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_prices", x => x.id);
                    table.CheckConstraint("ck_item_price_amount", "unit_price >= 0 AND coverage_percent >= 0 AND coverage_percent <= 100");
                    table.ForeignKey(
                        name: "fk_item_prices_hmo_companies_hmo_company_id",
                        column: x => x.hmo_company_id,
                        principalSchema: "clinic",
                        principalTable: "hmo_companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_item_prices_stock_item_stock_item_id",
                        column: x => x.stock_item_id,
                        principalSchema: "clinic",
                        principalTable: "stock_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_number = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ordered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    funding_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hmo_enrollment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hmo_company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    item_price_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price_snapshot = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    priority = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    clinical_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    bill_line_created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_requests", x => x.id);
                    table.CheckConstraint("ck_service_request_price", "unit_price_snapshot >= 0");
                    table.ForeignKey(
                        name: "fk_service_requests_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalSchema: "clinic",
                        principalTable: "encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_requests_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "clinic",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_requests_stock_item_stock_item_id",
                        column: x => x.stock_item_id,
                        principalSchema: "clinic",
                        principalTable: "stock_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    reorder_level = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_balances", x => x.id);
                    table.CheckConstraint("ck_stock_balance", "quantity_on_hand >= 0 AND reorder_level >= 0");
                    table.ForeignKey(
                        name: "fk_stock_balances_stock_item_stock_item_id",
                        column: x => x.stock_item_id,
                        principalSchema: "clinic",
                        principalTable: "stock_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_results",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    result_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    findings = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    structured_json = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    abnormal_flag = table.Column<bool>(type: "boolean", nullable: true),
                    performed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    entered_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    submitted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_results_service_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "clinic",
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_result_revisions",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    before_json = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    after_json = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_result_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_result_revisions_service_results_result_id",
                        column: x => x.result_id,
                        principalSchema: "clinic",
                        principalTable: "service_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_encounter_funding",
                schema: "clinic",
                table: "encounters",
                sql: "(funding_source = 'Private' AND hmo_enrollment_id IS NULL) OR (funding_source = 'Hmo' AND hmo_enrollment_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_hmo_enrollment_id",
                schema: "clinic",
                table: "appointments",
                column: "hmo_enrollment_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_appointment_funding",
                schema: "clinic",
                table: "appointments",
                sql: "(funding_source = 'Private' AND hmo_enrollment_id IS NULL) OR (funding_source = 'Hmo' AND hmo_enrollment_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_item_prices_hmo_company_id",
                schema: "clinic",
                table: "item_prices",
                column: "hmo_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_prices_stock_item_id_funding_source_hmo_company_id_eff",
                schema: "clinic",
                table: "item_prices",
                columns: new[] { "stock_item_id", "funding_source", "hmo_company_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_encounter_id_kind_status",
                schema: "clinic",
                table: "service_requests",
                columns: new[] { "encounter_id", "kind", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_kind_status",
                schema: "clinic",
                table: "service_requests",
                columns: new[] { "kind", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_patient_id",
                schema: "clinic",
                table: "service_requests",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_request_number",
                schema: "clinic",
                table: "service_requests",
                column: "request_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_stock_item_id",
                schema: "clinic",
                table: "service_requests",
                column: "stock_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_result_revisions_result_id_version",
                schema: "clinic",
                table: "service_result_revisions",
                columns: new[] { "result_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_results_request_id",
                schema: "clinic",
                table: "service_results",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_results_status",
                schema: "clinic",
                table: "service_results",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_stock_item_id",
                schema: "clinic",
                table: "stock_balances",
                column: "stock_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_item_code",
                schema: "clinic",
                table: "stock_item",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_item_name_is_active",
                schema: "clinic",
                table: "stock_item",
                columns: new[] { "name", "is_active" });

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_patient_hmo_enrollments_hmo_enrollment_id",
                schema: "clinic",
                table: "appointments",
                column: "hmo_enrollment_id",
                principalSchema: "clinic",
                principalTable: "patient_hmo_enrollments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_dispense_items_stock_item_stock_item_id",
                schema: "clinic",
                table: "dispense_items",
                column: "stock_item_id",
                principalSchema: "clinic",
                principalTable: "stock_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_investigation_orders_stock_item_catalog_item_id",
                schema: "clinic",
                table: "investigation_orders",
                column: "catalog_item_id",
                principalSchema: "clinic",
                principalTable: "stock_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_prescription_items_stock_item_stock_item_id",
                schema: "clinic",
                table: "prescription_items",
                column: "stock_item_id",
                principalSchema: "clinic",
                principalTable: "stock_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_stock_item_stock_item_id",
                schema: "clinic",
                table: "stock_movements",
                column: "stock_item_id",
                principalSchema: "clinic",
                principalTable: "stock_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_surgery_cases_stock_item_catalog_item_id",
                schema: "clinic",
                table: "surgery_cases",
                column: "catalog_item_id",
                principalSchema: "clinic",
                principalTable: "stock_item",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_appointments_patient_hmo_enrollments_hmo_enrollment_id",
                schema: "clinic",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "fk_dispense_items_stock_item_stock_item_id",
                schema: "clinic",
                table: "dispense_items");

            migrationBuilder.DropForeignKey(
                name: "fk_investigation_orders_stock_item_catalog_item_id",
                schema: "clinic",
                table: "investigation_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_prescription_items_stock_item_stock_item_id",
                schema: "clinic",
                table: "prescription_items");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_stock_item_stock_item_id",
                schema: "clinic",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_surgery_cases_stock_item_catalog_item_id",
                schema: "clinic",
                table: "surgery_cases");

            migrationBuilder.DropTable(
                name: "item_prices",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "service_result_revisions",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "stock_balances",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "service_results",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "service_requests",
                schema: "clinic");

            migrationBuilder.DropTable(
                name: "stock_item",
                schema: "clinic");

            migrationBuilder.DropCheckConstraint(
                name: "ck_encounter_funding",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropIndex(
                name: "ix_appointments_hmo_enrollment_id",
                schema: "clinic",
                table: "appointments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_appointment_funding",
                schema: "clinic",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "funding_source",
                schema: "clinic",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "funding_source",
                schema: "clinic",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "hmo_enrollment_id",
                schema: "clinic",
                table: "appointments");

            migrationBuilder.DropSequence(
                name: "service_request_number_seq",
                schema: "clinic");

            migrationBuilder.RenameColumn(
                name: "stock_item_id",
                schema: "clinic",
                table: "stock_movements",
                newName: "medication_id");

            migrationBuilder.RenameIndex(
                name: "ix_stock_movements_stock_item_id_created_at",
                schema: "clinic",
                table: "stock_movements",
                newName: "ix_stock_movements_medication_id_created_at");

            migrationBuilder.RenameColumn(
                name: "stock_item_id",
                schema: "clinic",
                table: "prescription_items",
                newName: "medication_id");

            migrationBuilder.RenameIndex(
                name: "ix_prescription_items_stock_item_id",
                schema: "clinic",
                table: "prescription_items",
                newName: "ix_prescription_items_medication_id");

            migrationBuilder.RenameColumn(
                name: "stock_item_id",
                schema: "clinic",
                table: "dispense_items",
                newName: "medication_id");

            migrationBuilder.RenameIndex(
                name: "ix_dispense_items_stock_item_id",
                schema: "clinic",
                table: "dispense_items",
                newName: "ix_dispense_items_medication_id");

            migrationBuilder.CreateTable(
                name: "investigation_catalog_items",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    tariff_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
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
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    form = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    generic_name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reorder_level = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    stock_on_hand = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    strength = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tariff_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    unit = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_medications", x => x.id);
                    table.CheckConstraint("ck_medication_price", "unit_price >= 0");
                    table.CheckConstraint("ck_medication_stock", "stock_on_hand >= 0");
                });

            migrationBuilder.CreateTable(
                name: "surgery_catalog_items",
                schema: "clinic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    tariff_code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surgery_catalog_items", x => x.id);
                    table.CheckConstraint("ck_surgery_price", "price >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_investigation_catalog_items_code",
                schema: "clinic",
                table: "investigation_catalog_items",
                column: "code",
                unique: true);

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
                name: "ix_surgery_catalog_items_code",
                schema: "clinic",
                table: "surgery_catalog_items",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_dispense_items_medications_medication_id",
                schema: "clinic",
                table: "dispense_items",
                column: "medication_id",
                principalSchema: "clinic",
                principalTable: "medications",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_investigation_orders_investigation_catalog_items_catalog_it",
                schema: "clinic",
                table: "investigation_orders",
                column: "catalog_item_id",
                principalSchema: "clinic",
                principalTable: "investigation_catalog_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_prescription_items_medications_medication_id",
                schema: "clinic",
                table: "prescription_items",
                column: "medication_id",
                principalSchema: "clinic",
                principalTable: "medications",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_medications_medication_id",
                schema: "clinic",
                table: "stock_movements",
                column: "medication_id",
                principalSchema: "clinic",
                principalTable: "medications",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_surgery_cases_surgery_catalog_items_catalog_item_id",
                schema: "clinic",
                table: "surgery_cases",
                column: "catalog_item_id",
                principalSchema: "clinic",
                principalTable: "surgery_catalog_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
