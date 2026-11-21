using Hms.Infrastructure.Persistence;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Hms.Infrastructure.Persistence;

/// <summary>EF Core model and Identity store for the clinic schema. Schema/index/constraint configuration lives here.</summary>
public class HmsDbContext(DbContextOptions<HmsDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public new DbSet<AppUser> Users => base.Users;
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ClinicSetting> ClinicSettings => Set<ClinicSetting>();
    public DbSet<FeeConfiguration> FeeConfigurations => Set<FeeConfiguration>();
    public DbSet<HmoCompany> HmoCompanies => Set<HmoCompany>();
    public DbSet<HmoContract> HmoContracts => Set<HmoContract>();
    public DbSet<HmoTariff> HmoTariffs => Set<HmoTariff>();
    public DbSet<HmoTariffItem> HmoTariffItems => Set<HmoTariffItem>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientRegistrationPeriod> PatientRegistrationPeriods => Set<PatientRegistrationPeriod>();
    public DbSet<PatientHmoEnrollment> PatientHmoEnrollments => Set<PatientHmoEnrollment>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Vital> Vitals => Set<Vital>();
    public DbSet<BedDay> BedDays => Set<BedDay>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Dispense> Dispenses => Set<Dispense>();
    public DbSet<DispenseItem> DispenseItems => Set<DispenseItem>();
    public DbSet<MedicationAdministration> MedicationAdministrations => Set<MedicationAdministration>();
    public DbSet<InvestigationCatalogItem> InvestigationCatalogItems => Set<InvestigationCatalogItem>();
    public DbSet<InvestigationOrder> InvestigationOrders => Set<InvestigationOrder>();
    public DbSet<InvestigationParticipant> InvestigationParticipants => Set<InvestigationParticipant>();
    public DbSet<SurgeryCatalogItem> SurgeryCatalogItems => Set<SurgeryCatalogItem>();
    public DbSet<SurgeryCase> SurgeryCases => Set<SurgeryCase>();
    public DbSet<SurgeryParticipant> SurgeryParticipants => Set<SurgeryParticipant>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillLine> BillLines => Set<BillLine>();
    public DbSet<BillAdjustment> BillAdjustments => Set<BillAdjustment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<HmoReceivable> HmoReceivables => Set<HmoReceivable>();
    public DbSet<PriorAuthorization> PriorAuthorizations => Set<PriorAuthorization>();
    public DbSet<BillStatusHistory> BillStatusHistories => Set<BillStatusHistory>();
    public DbSet<AccountBookEntry> AccountBookEntries => Set<AccountBookEntry>();
    public DbSet<FitnessCertificate> FitnessCertificates => Set<FitnessCertificate>();
    public DbSet<FitnessInvestigationResult> FitnessInvestigationResults => Set<FitnessInvestigationResult>();
    public DbSet<PatientFile> PatientFiles => Set<PatientFile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    // Global mapping choices: snake_case comes from the Npgsql naming convention;
    // enums are stored as readable strings and all money uses decimal(14,2).
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(1000);
        configurationBuilder.Properties<decimal>().HavePrecision(14, 2);

        configurationBuilder.Properties<StaffRole>().HaveConversion<string>().HaveMaxLength(40);
        configurationBuilder.Properties<Sex>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<PatientStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<AppointmentStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<EncounterType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<EncounterStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<AdmissionStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<WardType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<BillKind>().HaveConversion<string>().HaveMaxLength(40);
        configurationBuilder.Properties<BillStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PayerType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<BillLineCategory>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ChargeSourceType>().HaveConversion<string>().HaveMaxLength(40);
        configurationBuilder.Properties<PrescriptionStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DispenseStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<InvestigationStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<SurgeryStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<HmoContractStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TariffStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TariffCategory>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PriorAuthorizationStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<PaymentMethod>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<HmoReceivableStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<AdjustmentStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<AccountEntryType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<FitnessStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<FitnessPaymentStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<FitnessPaymentType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<StockMovementType>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<FeeCode>().HaveConversion<string>().HaveMaxLength(40);
    }

    // Schema map, indexes, checks and aggregate relationships. Billing invariants are
    // duplicated here where possible so direct SQL writes cannot bypass the API rules.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("clinic");

        // Keep Identity tables in the clinic schema but use compact names for core tables.
        modelBuilder.Entity<AppUser>().ToTable("users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        modelBuilder.Entity<AppUser>(b =>
        {
            b.HasIndex(x => x.EmployeeNumber).IsUnique().HasFilter("employee_number IS NOT NULL");
            b.HasIndex(x => new { x.StaffRole, x.IsActive });
            b.Property(x => x.SalaryAmount).HasPrecision(14, 2);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => new { x.UserId, x.ExpiresAt });
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClinicSetting>().HasKey(x => x.Key);

        modelBuilder.Entity<FeeConfiguration>(b =>
        {
            b.HasIndex(x => new { x.Code, x.EffectiveFrom }).IsUnique();
            b.HasIndex(x => new { x.Code, x.IsActive });
            b.ToTable(t => t.HasCheckConstraint("ck_fee_amount", "amount >= 0"));
            b.ToTable(t => t.HasCheckConstraint("ck_fee_dates", "effective_to IS NULL OR effective_to > effective_from"));
        });

        modelBuilder.Entity<HmoCompany>(b =>
        {
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.MetadataJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<HmoContract>(b =>
        {
            b.HasOne(x => x.HmoCompany).WithMany(x => x.Contracts).HasForeignKey(x => x.HmoCompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.HmoCompanyId, x.Status });
            b.HasIndex(x => x.EndDate);
            b.HasIndex(x => x.HmoCompanyId).IsUnique().HasFilter("status = 'Active'");
            b.ToTable(t => t.HasCheckConstraint("ck_hmo_contract_dates", "end_date > start_date"));
        });

        modelBuilder.Entity<HmoTariff>(b =>
        {
            b.HasOne(x => x.HmoCompany).WithMany(x => x.Tariffs).HasForeignKey(x => x.HmoCompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.HmoCompanyId, x.Version }).IsUnique();
            b.HasIndex(x => x.HmoCompanyId).IsUnique().HasFilter("status = 'Active'");
            b.ToTable(t => t.HasCheckConstraint("ck_hmo_tariff_dates", "effective_to IS NULL OR effective_to > effective_from"));
        });

        modelBuilder.Entity<HmoTariffItem>(b =>
        {
            b.HasOne(x => x.HmoTariff).WithMany(x => x.Items).HasForeignKey(x => x.HmoTariffId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.HmoTariffId, x.Code }).IsUnique();
            b.HasIndex(x => new { x.Category, x.Code, x.IsActive });
            b.ToTable(t => t.HasCheckConstraint("ck_tariff_price", "unit_price >= 0"));
            b.ToTable(t => t.HasCheckConstraint("ck_tariff_coverage", "coverage_percent >= 0 AND coverage_percent <= 100"));
        });

        modelBuilder.Entity<Patient>(b =>
        {
            b.HasIndex(x => x.PatientNumber).IsUnique();
            b.HasIndex(x => new { x.LastName, x.FirstName });
            b.HasIndex(x => x.Phone);
            b.HasIndex(x => x.CurrentRegistrationExpiresAt);
            b.ToTable(t => t.HasCheckConstraint("ck_patient_registration", "current_registration_expires_at > registration_date"));
        });

        modelBuilder.Entity<PatientRegistrationPeriod>(b =>
        {
            b.HasOne(x => x.Patient).WithMany(x => x.RegistrationPeriods).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.RenewalOf).WithMany().HasForeignKey(x => x.RenewalOfId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.PatientId).IsUnique().HasFilter("is_current = TRUE");
            b.HasIndex(x => x.PeriodEnd);
            b.ToTable(t => t.HasCheckConstraint("ck_registration_dates", "period_end > period_start"));
            b.ToTable(t => t.HasCheckConstraint("ck_registration_fee", "fee_amount >= 0"));
        });

        modelBuilder.Entity<PatientHmoEnrollment>(b =>
        {
            b.HasOne(x => x.Patient).WithMany(x => x.HmoEnrollments).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoCompany).WithMany(x => x.Enrollments).HasForeignKey(x => x.HmoCompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.HmoCompanyId, x.HmoMemberId }).IsUnique().HasFilter("is_active = TRUE");
            b.HasIndex(x => x.PatientId).IsUnique().HasFilter("is_primary = TRUE AND is_active = TRUE");
            b.ToTable(t => t.HasCheckConstraint("ck_enrollment_dates", "valid_to IS NULL OR valid_to >= valid_from"));
        });

        modelBuilder.Entity<Appointment>(b =>
        {
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.AppointmentNumber).IsUnique();
            b.HasIndex(x => new { x.DoctorId, x.ScheduledAt }).IsUnique()
                .HasFilter("status IN ('Scheduled', 'CheckedIn')");
            b.HasIndex(x => new { x.PatientId, x.ScheduledAt });
            b.ToTable(t => t.HasCheckConstraint("ck_appointment_duration", "duration_minutes > 0"));
        });

        modelBuilder.Entity<Admission>(b =>
        {
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AttendingDoctorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoEnrollment).WithMany().HasForeignKey(x => x.HmoEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.AdmissionNumber).IsUnique();
            b.HasIndex(x => x.PatientId).IsUnique().HasFilter("status IN ('Active', 'PendingDischarge')");
            b.HasIndex(x => new { x.PatientId, x.AdmittedAt });
            b.ToTable(t => t.HasCheckConstraint("ck_admission_discharge", "discharged_at IS NULL OR discharged_at >= admitted_at"));
        });

        modelBuilder.Entity<Encounter>(b =>
        {
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Admission).WithMany(x => x.Encounters).HasForeignKey(x => x.AdmissionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoEnrollment).WithMany().HasForeignKey(x => x.HmoEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.EncounterNumber).IsUnique();
            b.HasIndex(x => x.AppointmentId).IsUnique().HasFilter("appointment_id IS NOT NULL");
            b.HasIndex(x => new { x.PatientId, x.StartedAt });
            b.HasIndex(x => new { x.DoctorId, x.StartedAt });
            b.HasIndex(x => x.AdmissionId);
            b.ToTable(t => t.HasCheckConstraint("ck_encounter_admission",
                "(type = 'Outpatient' AND admission_id IS NULL) OR (type = 'Inpatient' AND admission_id IS NOT NULL)"));
        });

        modelBuilder.Entity<Vital>(b =>
        {
            b.HasOne(x => x.Encounter).WithMany(x => x.Vitals).HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.NurseId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EncounterId, x.RecordedAt });
            b.ToTable(t => t.HasCheckConstraint("ck_vitals_pain", "pain_score IS NULL OR (pain_score >= 0 AND pain_score <= 10)"));
        });

        modelBuilder.Entity<BedDay>(b =>
        {
            b.HasOne(x => x.Admission).WithMany(x => x.BedDays).HasForeignKey(x => x.AdmissionId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.AdmissionId, x.ServiceDate }).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("ck_bed_day_rate", "rate >= 0"));
        });

        modelBuilder.Entity<Medication>(b =>
        {
            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => new { x.Name, x.IsActive });
            b.ToTable(t => t.HasCheckConstraint("ck_medication_price", "unit_price >= 0"));
            b.ToTable(t => t.HasCheckConstraint("ck_medication_stock", "stock_on_hand >= 0"));
        });

        modelBuilder.Entity<StockMovement>(b =>
        {
            b.HasOne(x => x.Medication).WithMany().HasForeignKey(x => x.MedicationId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.MedicationId, x.CreatedAt });
        });

        modelBuilder.Entity<Prescription>(b =>
        {
            b.HasOne(x => x.Encounter).WithMany(x => x.Prescriptions).HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EncounterId, x.Status });
        });

        modelBuilder.Entity<PrescriptionItem>(b =>
        {
            b.HasOne(x => x.Prescription).WithMany(x => x.Items).HasForeignKey(x => x.PrescriptionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Medication).WithMany().HasForeignKey(x => x.MedicationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.QuantityPrescribed).HasPrecision(18, 3);
            b.Property(x => x.QuantityDispensed).HasPrecision(18, 3);
            b.ToTable(t => t.HasCheckConstraint("ck_prescription_quantity", "quantity_prescribed > 0 AND quantity_dispensed >= 0"));
        });

        modelBuilder.Entity<Dispense>(b =>
        {
            b.HasOne(x => x.Prescription).WithMany(x => x.Dispenses).HasForeignKey(x => x.PrescriptionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Encounter).WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.PharmacyUserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EncounterId, x.CreatedAt });
        });

        modelBuilder.Entity<DispenseItem>(b =>
        {
            b.HasOne(x => x.Dispense).WithMany(x => x.Items).HasForeignKey(x => x.DispenseId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.PrescriptionItem).WithMany().HasForeignKey(x => x.PrescriptionItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Medication).WithMany().HasForeignKey(x => x.MedicationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.TariffItem).WithMany().HasForeignKey(x => x.TariffItemId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Quantity).HasPrecision(18, 3);
            b.ToTable(t => t.HasCheckConstraint("ck_dispense_quantity", "quantity > 0"));
            b.ToTable(t => t.HasCheckConstraint("ck_dispense_amounts", "unit_price >= 0 AND hmo_amount >= 0 AND patient_amount >= 0"));
        });

        modelBuilder.Entity<MedicationAdministration>(b =>
        {
            b.HasOne(x => x.Encounter).WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.PrescriptionItem).WithMany().HasForeignKey(x => x.PrescriptionItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.DispenseItem).WithMany(x => x.Administrations).HasForeignKey(x => x.DispenseItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.NurseId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EncounterId, x.AdministeredAt });
            b.ToTable(t => t.HasCheckConstraint("ck_administration_dose", "dose_given > 0"));
        });

        modelBuilder.Entity<InvestigationCatalogItem>(b =>
        {
            b.HasIndex(x => x.Code).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("ck_investigation_price", "price >= 0"));
        });

        modelBuilder.Entity<InvestigationOrder>(b =>
        {
            b.HasOne(x => x.Encounter).WithMany(x => x.Investigations).HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.CatalogItem).WithMany().HasForeignKey(x => x.CatalogItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EncounterId, x.Status });
        });

        modelBuilder.Entity<InvestigationParticipant>(b =>
        {
            b.HasOne(x => x.InvestigationOrder).WithMany(x => x.Participants).HasForeignKey(x => x.InvestigationOrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.InvestigationOrderId, x.UserId, x.ProcedureRole }).IsUnique();
        });

        modelBuilder.Entity<SurgeryCatalogItem>(b =>
        {
            b.HasIndex(x => x.Code).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("ck_surgery_price", "price >= 0"));
        });

        modelBuilder.Entity<SurgeryCase>(b =>
        {
            b.HasOne(x => x.Encounter).WithMany(x => x.Surgeries).HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.LeadSurgeonId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.CatalogItem).WithMany().HasForeignKey(x => x.CatalogItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EncounterId, x.Status });
        });

        modelBuilder.Entity<SurgeryParticipant>(b =>
        {
            b.HasOne(x => x.SurgeryCase).WithMany(x => x.Participants).HasForeignKey(x => x.SurgeryCaseId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.SurgeryCaseId, x.UserId, x.ProcedureRole }).IsUnique();
        });

        modelBuilder.Entity<Bill>(b =>
        {
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Encounter).WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Admission).WithMany().HasForeignKey(x => x.AdmissionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.RegistrationPeriod).WithMany().HasForeignKey(x => x.RegistrationPeriodId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.FitnessCertificate).WithMany().HasForeignKey(x => x.FitnessCertificateId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoEnrollment).WithMany().HasForeignKey(x => x.HmoEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoCompany).WithMany().HasForeignKey(x => x.HmoCompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.PriorAuthorization).WithMany().HasForeignKey(x => x.PriorAuthorizationId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.BillNumber).IsUnique();
            b.HasIndex(x => new { x.Status, x.CreatedAt });
            b.HasIndex(x => new { x.PatientId, x.CreatedAt });
            b.HasIndex(x => new { x.HmoCompanyId, x.Status });
            b.HasIndex(x => x.EncounterId).IsUnique().HasFilter("kind = 'OutpatientEncounter' AND status <> 'Void'");
            b.HasIndex(x => x.AdmissionId).IsUnique().HasFilter("kind = 'InpatientAdmission' AND status <> 'Void'");
            b.HasIndex(x => x.AppointmentId).IsUnique().HasFilter("kind = 'Appointment' AND status <> 'Void'");
            b.HasIndex(x => x.RegistrationPeriodId).IsUnique().HasFilter("kind = 'Registration' AND status <> 'Void'");
            b.HasIndex(x => x.FitnessCertificateId).IsUnique().HasFilter("kind = 'FitnessCertificate' AND status <> 'Void'");
            b.HasIndex(x => x.EncounterId).IsUnique().HasFilter("kind = 'Emergency' AND status <> 'Void'");
            b.ToTable(t =>
            {
                t.HasCheckConstraint("ck_bill_finance_approval", "status NOT IN ('Approved', 'Paid') OR finance_approved_at IS NOT NULL");
                t.HasCheckConstraint("ck_bill_hmo_approval", "status NOT IN ('HmoApproved', 'Approved', 'Paid') OR payer_type <> 'Hmo' OR hmo_approved_at IS NOT NULL");
                t.HasCheckConstraint("ck_bill_hmo_context", "payer_type <> 'Hmo' OR hmo_company_id IS NOT NULL");
                t.HasCheckConstraint("ck_bill_context",
                    "(kind = 'OutpatientEncounter' AND encounter_id IS NOT NULL) OR (kind = 'Emergency' AND encounter_id IS NOT NULL) OR (kind = 'InpatientAdmission' AND admission_id IS NOT NULL) OR (kind = 'Appointment' AND appointment_id IS NOT NULL) OR (kind = 'Registration' AND registration_period_id IS NOT NULL) OR (kind = 'FitnessCertificate' AND fitness_certificate_id IS NOT NULL)");
            });
        });

        modelBuilder.Entity<BillLine>(b =>
        {
            b.HasOne(x => x.Bill).WithMany(x => x.Lines).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.TariffItem).WithMany().HasForeignKey(x => x.TariffItemId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Encounter).WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Admission).WithMany().HasForeignKey(x => x.AdmissionId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.SourceKey).IsUnique();
            b.HasIndex(x => new { x.BillId, x.Category });
            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.AdmissionId);
            b.Property(x => x.Quantity).HasPrecision(18, 3);
            b.ToTable(t =>
            {
                t.HasCheckConstraint("ck_bill_line_arithmetic",
                    "net_amount = gross_amount - discount_amount AND net_amount = hmo_amount + patient_amount");
                t.HasCheckConstraint("ck_bill_line_amounts", "quantity > 0 AND unit_price >= 0 AND gross_amount >= 0 AND discount_amount >= 0 AND net_amount >= 0 AND hmo_amount >= 0 AND patient_amount >= 0");
            });
        });

        modelBuilder.Entity<BillAdjustment>(b =>
        {
            b.HasOne(x => x.Bill).WithMany(x => x.Adjustments).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.BillId, x.Status });
            b.ToTable(t => t.HasCheckConstraint("ck_adjustment_amount", "amount >= 0"));
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.HasOne(x => x.Bill).WithMany(x => x.Payments).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.BillId, x.ReceivedAt });
            b.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
            b.ToTable(t => t.HasCheckConstraint("ck_payment_amount", "amount > 0"));
        });

        modelBuilder.Entity<HmoReceivable>(b =>
        {
            b.HasOne(x => x.Bill).WithOne(x => x.Receivable).HasForeignKey<HmoReceivable>(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoCompany).WithMany().HasForeignKey(x => x.HmoCompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.PriorAuthorization).WithMany().HasForeignKey(x => x.PriorAuthorizationId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.BillId).IsUnique();
            b.HasIndex(x => new { x.Status, x.DueDate });
        });

        modelBuilder.Entity<PriorAuthorization>(b =>
        {
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoCompany).WithMany().HasForeignKey(x => x.HmoCompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoEnrollment).WithMany().HasForeignKey(x => x.HmoEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Encounter).WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Admission).WithMany().HasForeignKey(x => x.AdmissionId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.HmoCompanyId, x.PaCode }).IsUnique();
            b.HasIndex(x => new { x.EncounterId, x.AdmissionId });
            b.ToTable(t => t.HasCheckConstraint("ck_pa_context",
                "(encounter_id IS NOT NULL AND admission_id IS NULL) OR (encounter_id IS NULL AND admission_id IS NOT NULL)"));
        });

        modelBuilder.Entity<BillStatusHistory>(b =>
        {
            b.HasOne(x => x.Bill).WithMany(x => x.StatusHistory).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.BillId, x.OccurredAt });
        });

        modelBuilder.Entity<AccountBookEntry>(b =>
        {
            b.HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoReceivable).WithMany().HasForeignKey(x => x.HmoReceivableId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.EntryNumber).IsUnique();
            b.HasIndex(x => x.PaymentId).IsUnique().HasFilter("payment_id IS NOT NULL");
            b.HasIndex(x => new { x.OccurredAt, x.EntryType });
            b.ToTable(t => t.HasCheckConstraint("ck_account_amount", "amount > 0"));
        });

        modelBuilder.Entity<FitnessCertificate>(b =>
        {
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoCompany).WithMany().HasForeignKey(x => x.HmoCompanyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.HmoEnrollment).WithMany().HasForeignKey(x => x.HmoEnrollmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.CertificateNumber).IsUnique();
            b.HasIndex(x => new { x.PatientId, x.FitnessDate });
            b.HasIndex(x => new { x.FitnessDate, x.HmoCompanyId, x.Status });
            b.ToTable(t => t.HasCheckConstraint("ck_fitness_age", "patient_age >= 0 AND patient_age <= 130"));
        });

        modelBuilder.Entity<FitnessInvestigationResult>(b =>
        {
            b.HasOne(x => x.FitnessCertificate).WithMany(x => x.InvestigationResults)
                .HasForeignKey(x => x.FitnessCertificateId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.FitnessCertificateId, x.SortOrder });
        });

        modelBuilder.Entity<PatientFile>(b =>
        {
            b.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.PatientId, x.CreatedAt });
            b.Property(x => x.StorageKey).HasMaxLength(500);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasIndex(x => x.OccurredAt);
            b.HasIndex(x => new { x.EntityType, x.EntityId });
            b.HasIndex(x => new { x.ActorUserId, x.OccurredAt });
            b.HasIndex(x => x.BillId);
            b.Property(x => x.BeforeJson).HasColumnType("jsonb");
            b.Property(x => x.AfterJson).HasColumnType("jsonb");
        });

        // Human-readable document numbers are database sequences, not MAX(id)+1.
        modelBuilder.HasSequence<long>("patient_number_seq", "clinic").StartsAt(1000);
        modelBuilder.HasSequence<long>("appointment_number_seq", "clinic").StartsAt(1000);
        modelBuilder.HasSequence<long>("encounter_number_seq", "clinic").StartsAt(1000);
        modelBuilder.HasSequence<long>("admission_number_seq", "clinic").StartsAt(1000);
        modelBuilder.HasSequence<long>("bill_number_seq", "clinic").StartsAt(1000);
        modelBuilder.HasSequence<long>("certificate_number_seq", "clinic").StartsAt(1000);
        modelBuilder.HasSequence<long>("account_entry_seq", "clinic").StartsAt(1000);
    }
}
