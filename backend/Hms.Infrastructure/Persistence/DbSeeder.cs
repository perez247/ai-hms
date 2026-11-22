using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hms.Infrastructure.Persistence;

/// <summary>
/// Idempotent development seed. It applies migrations, creates one account per role,
/// configures fees/HMO/tariffs, and seeds one outpatient and one inpatient demo journey.
/// </summary>
public static class DbSeeder
{
    public const string DefaultPassword = "ChangeMe123!";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await db.Database.MigrateAsync(cancellationToken);
        if (await db.Users.AnyAsync(cancellationToken)) return;

        // Stop at the first user: this keeps restarts idempotent without touching demo data.
        logger.LogInformation("Seeding clinic database");
        var users = new Dictionary<StaffRole, AppUser>();
        foreach (var role in Enum.GetValues<StaffRole>())
        {
            var email = $"{ToEmailPrefix(role)}@clinic.local";
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = $"{role} Demo",
                StaffRole = role,
                EmployeeNumber = $"EMP-{role.ToString().ToUpperInvariant()}",
                IsActive = true,
                HiredOn = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                SalaryAmount = role == StaffRole.Admin ? 1_500_000m : 800_000m,
                BankName = "Demo Bank",
                BankAccountName = $"{role} Demo",
                BankAccountNumber = $"000000{((int)role + 1):D2}"
            };
            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not seed {role}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            users[role] = user;
        }

        var adminId = users[StaffRole.Admin].Id;
        var doctorId = users[StaffRole.Doctor].Id;
        var nurseId = users[StaffRole.Nurse].Id;
        var receptionId = users[StaffRole.Receptionist].Id;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        db.ClinicSettings.AddRange(
            new ClinicSetting { Key = "registration.validity_months", ValueJson = "24", Description = "Registration validity in months." },
            new ClinicSetting { Key = "hmo.contract_term_months", ValueJson = "60", Description = "Default HMO contract term in months." },
            new ClinicSetting { Key = "billing.currency", ValueJson = "\"NGN\"", Description = "Default billing currency." },
            new ClinicSetting { Key = "clinic.timezone", ValueJson = "\"Africa/Lagos\"", Description = "Clinic operating timezone." },
            new ClinicSetting { Key = "billing.bed_day_mode", ValueJson = "\"per_night\"", Description = "Bed-day billing mode." },
            new ClinicSetting { Key = "alerts.expiry_warning_days", ValueJson = "[30,7]", Description = "Registration and contract warning windows." });

        db.FeeConfigurations.AddRange(
            Fee(FeeCode.RegistrationNew, 10_000m, adminId, today.AddYears(-1)),
            Fee(FeeCode.RegistrationRenewal, 7_500m, adminId, today.AddYears(-1)),
            Fee(FeeCode.Appointment, 5_000m, adminId, today.AddYears(-1)),
            Fee(FeeCode.Emergency, 20_000m, adminId, today.AddYears(-1)),
            Fee(FeeCode.InpatientConsultation, 15_000m, adminId, today.AddYears(-1)),
            Fee(FeeCode.BedDayGeneral, 12_000m, adminId, today.AddYears(-1)),
            Fee(FeeCode.BedDayPrivate, 30_000m, adminId, today.AddYears(-1)),
            Fee(FeeCode.FitnessCertificate, 8_000m, adminId, today.AddYears(-1)));

        // Partner, contract and tariff data used by the HMO demo path.
        var hmo = new HmoCompany
        {
            Code = "HEALTH-DEMO",
            Name = "HealthGuard HMO",
            ContactPerson = "HMO Desk",
            Email = "claims@healthguard.example",
            Phone = "+234-800-000-0000",
            Address = "1 Insurance Way, Lagos",
            ContractTerms = "Demo contract. 80% medication cover, 100% bed-day cover.",
            CreatedBy = adminId
        };
        db.HmoCompanies.Add(hmo);
        await db.SaveChangesAsync(cancellationToken);

        var contract = new HmoContract
        {
            HmoCompanyId = hmo.Id,
            StartDate = today,
            EndDate = today.AddYears(5),
            Status = HmoContractStatus.Active,
            Terms = "Demo five-year contract",
            CreatedBy = adminId
        };
        db.HmoContracts.Add(contract);

        var tariff = new HmoTariff
        {
            HmoCompanyId = hmo.Id,
            Name = "HealthGuard standard tariff",
            Version = 1,
            EffectiveFrom = today.AddDays(-1),
            Status = TariffStatus.Active,
            SourceFileName = "seed-tariff.csv",
            ImportedBy = adminId
        };
        tariff.Items.Add(new HmoTariffItem { Code = "PARACETAMOL", Description = "Paracetamol 500mg", Category = TariffCategory.Medication, Unit = "tablet", UnitPrice = 400m, CoveragePercent = 80m });
        tariff.Items.Add(new HmoTariffItem { Code = "AMOXICILLIN", Description = "Amoxicillin 500mg", Category = TariffCategory.Medication, Unit = "capsule", UnitPrice = 1_000m, CoveragePercent = 80m });
        tariff.Items.Add(new HmoTariffItem { Code = "INPATIENT_CONSULTATION", Description = "Inpatient consultation", Category = TariffCategory.Consultation, Unit = "visit", UnitPrice = 15_000m, CoveragePercent = 100m });
        tariff.Items.Add(new HmoTariffItem { Code = "BED_DAY_GENERAL", Description = "General ward bed-day", Category = TariffCategory.BedDay, Unit = "night", UnitPrice = 12_000m, CoveragePercent = 100m });
        tariff.Items.Add(new HmoTariffItem { Code = "BED_DAY_PRIVATE", Description = "Private ward bed-day", Category = TariffCategory.BedDay, Unit = "night", UnitPrice = 30_000m, CoveragePercent = 100m });
        tariff.Items.Add(new HmoTariffItem { Code = "CBC", Description = "Complete blood count", Category = TariffCategory.Investigation, Unit = "test", UnitPrice = 5_000m, CoveragePercent = 100m });
        tariff.Items.Add(new HmoTariffItem { Code = "ULTRASOUND", Description = "Ultrasound scan", Category = TariffCategory.Investigation, Unit = "scan", UnitPrice = 15_000m, CoveragePercent = 90m });
        tariff.Items.Add(new HmoTariffItem { Code = "APPENDECTOMY", Description = "Appendectomy", Category = TariffCategory.Surgery, Unit = "procedure", UnitPrice = 250_000m, CoveragePercent = 90m });
        db.HmoTariffs.Add(tariff);

        var paracetamol = new Medication { Code = "PARA500", Name = "Paracetamol", GenericName = "Paracetamol", Form = "Tablet", Strength = "500mg", Unit = "tablet", UnitPrice = 500m, TariffCode = "PARACETAMOL", StockOnHand = 1000m, ReorderLevel = 100m };
        var amoxicillin = new Medication { Code = "AMOX500", Name = "Amoxicillin", GenericName = "Amoxicillin", Form = "Capsule", Strength = "500mg", Unit = "capsule", UnitPrice = 1_200m, TariffCode = "AMOXICILLIN", StockOnHand = 500m, ReorderLevel = 50m };
        var artemether = new Medication { Code = "ART20", Name = "Artemether/Lumefantrine", Form = "Tablet", Strength = "20/120mg", Unit = "tablet", UnitPrice = 1_500m, TariffCode = "ARTEMETHER", StockOnHand = 300m, ReorderLevel = 40m };
        db.Medications.AddRange(paracetamol, amoxicillin, artemether);

        var cbc = new InvestigationCatalogItem { Code = "CBC", Name = "Complete blood count", Category = "Laboratory", Price = 5_000m, TariffCode = "CBC" };
        var ultrasound = new InvestigationCatalogItem { Code = "US", Name = "Ultrasound scan", Category = "Radiology", Price = 15_000m, TariffCode = "ULTRASOUND" };
        db.InvestigationCatalogItems.AddRange(cbc, ultrasound);
        var appendectomy = new SurgeryCatalogItem { Code = "APPEND", Name = "Appendectomy", Price = 250_000m, TariffCode = "APPENDECTOMY" };
        db.SurgeryCatalogItems.Add(appendectomy);

        // Demo patients: Ada exercises HMO outpatient billing; Chinedu exercises inpatient discharge.
        var ada = new Patient
        {
            PatientNumber = "PAT-DEMO-0001", FirstName = "Ada", LastName = "Okafor", Sex = Sex.Female,
            DateOfBirth = new DateOnly(1992, 5, 18), Phone = "+234-801-000-0001", Email = "ada@example.test",
            Address = "12 Palm Street, Lagos", NextOfKinName = "Ngozi Okafor", NextOfKinPhone = "+234-801-000-0002",
            RegistrationDate = today, CurrentRegistrationExpiresAt = today.AddYears(2), Status = PatientStatus.Active, CreatedBy = receptionId
        };
        var chinedu = new Patient
        {
            PatientNumber = "PAT-DEMO-0002", FirstName = "Chinedu", LastName = "Bello", Sex = Sex.Male,
            DateOfBirth = new DateOnly(1984, 11, 2), Phone = "+234-802-000-0001", Address = "4 Market Road, Ibadan",
            RegistrationDate = today, CurrentRegistrationExpiresAt = today.AddYears(2), Status = PatientStatus.Active, CreatedBy = receptionId
        };
        db.Patients.AddRange(ada, chinedu);
        await db.SaveChangesAsync(cancellationToken);

        db.PatientRegistrationPeriods.AddRange(
            new PatientRegistrationPeriod { PatientId = ada.Id, PeriodStart = today, PeriodEnd = today.AddYears(2), FeeAmount = 10_000m, IsCurrent = true, ProcessedBy = receptionId },
            new PatientRegistrationPeriod { PatientId = chinedu.Id, PeriodStart = today, PeriodEnd = today.AddYears(2), FeeAmount = 10_000m, IsCurrent = true, ProcessedBy = receptionId });

        var enrollment = new PatientHmoEnrollment
        {
            PatientId = ada.Id, HmoCompanyId = hmo.Id, HmoMemberId = "HG-000123", PlanName = "Standard",
            ValidFrom = today.AddMonths(-2), ValidTo = today.AddYears(1), IsPrimary = true, CreatedBy = receptionId
        };
        db.PatientHmoEnrollments.Add(enrollment);
        await db.SaveChangesAsync(cancellationToken);

        var appointment = new Appointment
        {
            AppointmentNumber = "APT-DEMO-0001", PatientId = ada.Id, DoctorId = doctorId,
            ScheduledAt = now.AddMinutes(-30), DurationMinutes = 30, Reason = "Fever and headache",
            Status = AppointmentStatus.CheckedIn, CheckedInAt = now.AddMinutes(-25), CreatedBy = receptionId
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        var admission = new Admission
        {
            AdmissionNumber = "ADM-DEMO-0001", PatientId = chinedu.Id, AttendingDoctorId = doctorId,
            WardType = WardType.General, BedNumber = "G-04", AdmittedAt = now.AddDays(-2),
            Status = AdmissionStatus.Active, CreatedBy = doctorId
        };
        db.Admissions.Add(admission);
        await db.SaveChangesAsync(cancellationToken);

        var outpatientEncounter = new Encounter
        {
            EncounterNumber = "ENC-DEMO-0001", PatientId = ada.Id, DoctorId = doctorId, AppointmentId = appointment.Id,
            Type = EncounterType.Outpatient, HmoEnrollmentId = enrollment.Id, Status = EncounterStatus.Open,
            ChiefComplaint = "Fever and headache", CreatedBy = doctorId
        };
        var inpatientEncounter = new Encounter
        {
            EncounterNumber = "ENC-DEMO-0002", PatientId = chinedu.Id, DoctorId = doctorId, AdmissionId = admission.Id,
            Type = EncounterType.Inpatient, Status = EncounterStatus.Open, ChiefComplaint = "Abdominal pain", CreatedBy = doctorId
        };
        db.Encounters.AddRange(outpatientEncounter, inpatientEncounter);
        await db.SaveChangesAsync(cancellationToken);

        var appointmentBill = NewBill("BILL-DEMO-0001", ada.Id, BillKind.Appointment, appointment.Id, null, null, null, null,
            PayerType.SelfPay, null, null, BillStatus.Committed, adminId, now);
        appointmentBill.Lines.Add(NewLine(appointmentBill.Id, BillLineCategory.Appointment, "Appointment fee", 5_000m,
            ChargeSourceType.Appointment, appointment.Id, $"appointment:{appointment.Id}", null, null, adminId));
        BillingCalculator.Recalculate(appointmentBill);

        var outpatientBill = NewBill("BILL-DEMO-0002", ada.Id, BillKind.OutpatientEncounter, null, outpatientEncounter.Id, null, null, null,
            PayerType.Hmo, hmo.Id, null, BillStatus.Draft, doctorId, now);
        outpatientBill.HmoEnrollmentId = enrollment.Id;

        var inpatientBill = NewBill("BILL-DEMO-0003", chinedu.Id, BillKind.InpatientAdmission, null, null, admission.Id, null, null,
            PayerType.SelfPay, null, null, BillStatus.Draft, doctorId, now);
        inpatientBill.Lines.Add(NewLine(inpatientBill.Id, BillLineCategory.Consultation, "Inpatient consultation ENC-DEMO-0002",
            15_000m, ChargeSourceType.Encounter, inpatientEncounter.Id, $"consultation:{inpatientEncounter.Id}", inpatientEncounter.Id, admission.Id, doctorId));
        BillingCalculator.Recalculate(inpatientBill);

        db.Bills.AddRange(appointmentBill, outpatientBill, inpatientBill);
        db.BillStatusHistories.AddRange(
            new BillStatusHistory { BillId = appointmentBill.Id, FromStatus = null, ToStatus = BillStatus.Committed, ActorId = adminId, Reason = "Seed" },
            new BillStatusHistory { BillId = outpatientBill.Id, FromStatus = null, ToStatus = BillStatus.Draft, ActorId = doctorId, Reason = "Seed" },
            new BillStatusHistory { BillId = inpatientBill.Id, FromStatus = null, ToStatus = BillStatus.Draft, ActorId = doctorId, Reason = "Seed" });

        var prescription = new Prescription
        {
            EncounterId = outpatientEncounter.Id, DoctorId = doctorId, Status = PrescriptionStatus.SentToPharmacy,
            SentToPharmacyAt = now, Notes = "Take after food", CreatedBy = doctorId
        };
        prescription.Items.Add(new PrescriptionItem { MedicationId = paracetamol.Id, Dose = "1 tablet", Route = "Oral", Frequency = "TDS", DurationDays = 3, QuantityPrescribed = 9m });
        prescription.Items.Add(new PrescriptionItem { MedicationId = amoxicillin.Id, Dose = "1 capsule", Route = "Oral", Frequency = "BD", DurationDays = 5, QuantityPrescribed = 10m });
        db.Prescriptions.Add(prescription);

        var inpatientPrescription = new Prescription
        {
            EncounterId = inpatientEncounter.Id, DoctorId = doctorId, Status = PrescriptionStatus.SentToPharmacy,
            SentToPharmacyAt = now, CreatedBy = doctorId
        };
        inpatientPrescription.Items.Add(new PrescriptionItem { MedicationId = artemether.Id, Dose = "2 tablets", Route = "Oral", Frequency = "BD", DurationDays = 3, QuantityPrescribed = 12m });
        db.Prescriptions.Add(inpatientPrescription);

        db.Vitals.Add(new Vital
        {
            PatientId = ada.Id, EncounterId = outpatientEncounter.Id, NurseId = nurseId, TemperatureC = 38.2m, Systolic = 118, Diastolic = 78,
            Pulse = 92, RespiratoryRate = 18, Spo2 = 98, WeightKg = 68m, HeightCm = 165m, PainScore = 4,
            Notes = "Febrile, alert", CreatedBy = nurseId
        });
        db.Vitals.Add(new Vital
        {
            PatientId = chinedu.Id, EncounterId = inpatientEncounter.Id, NurseId = nurseId, TemperatureC = 37.4m, Systolic = 126, Diastolic = 82,
            Pulse = 84, RespiratoryRate = 16, Spo2 = 99, WeightKg = 82m, HeightCm = 178m, PainScore = 6,
            Notes = "Abdominal tenderness", CreatedBy = nurseId
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed complete. Users: {Email} / {Password}", "admin@clinic.local", DefaultPassword);
    }

    private static FeeConfiguration Fee(FeeCode code, decimal amount, Guid adminId, DateOnly effectiveFrom) => new()
    {
        Code = code, Amount = amount, Currency = "NGN", EffectiveFrom = effectiveFrom, CreatedBy = adminId
    };

    private static Bill NewBill(string number, Guid patientId, BillKind kind, Guid? appointmentId, Guid? encounterId,
        Guid? admissionId, Guid? registrationPeriodId, Guid? fitnessCertificateId, PayerType payerType,
        Guid? hmoCompanyId, Guid? priorAuthorizationId, BillStatus status, Guid actorId, DateTimeOffset createdAt) => new()
    {
        BillNumber = number, PatientId = patientId, Kind = kind, AppointmentId = appointmentId, EncounterId = encounterId,
        AdmissionId = admissionId, RegistrationPeriodId = registrationPeriodId, FitnessCertificateId = fitnessCertificateId,
        PayerType = payerType, HmoCompanyId = hmoCompanyId, PriorAuthorizationId = priorAuthorizationId,
        Status = status, CommittedAt = status == BillStatus.Draft ? null : createdAt, CommittedBy = status == BillStatus.Draft ? null : actorId,
        CreatedBy = actorId, CreatedAt = createdAt
    };

    private static BillLine NewLine(Guid billId, BillLineCategory category, string description, decimal unitPrice,
        ChargeSourceType sourceType, Guid sourceId, string sourceKey, Guid? encounterId, Guid? admissionId, Guid actorId)
    {
        var line = new BillLine
        {
            BillId = billId, Category = category, Description = description, Quantity = 1m, UnitPrice = unitPrice,
            SourceType = sourceType, SourceId = sourceId, SourceKey = sourceKey, EncounterId = encounterId,
            AdmissionId = admissionId, CreatedBy = actorId
        };
        BillingCalculator.SetLineAmounts(line, 0m, false);
        return line;
    }

    private static string ToEmailPrefix(StaffRole role) => role switch
    {
        StaffRole.HmoOffice => "hmo",
        StaffRole.HrOffice => "hr",
        _ => role.ToString().ToLowerInvariant()
    };
}
