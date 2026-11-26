using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hms.Infrastructure.Persistence;

/// <summary>
/// Idempotent development seed. It applies migrations, creates one account per role and
/// configures fees/HMO/tariffs plus the stock catalogue. It deliberately creates no patient,
/// appointment, encounter or billing records; those are entered through the application.
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
        if (await db.Users.AnyAsync(cancellationToken))
        {
            // Existing installations still need the appointment reason catalogue.
            await EnsureAppointmentReasonPricingAsync(db, logger, cancellationToken);
            return;
        }

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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

        StockItem Medication(string code, string name, string? generic, string? form, string? strength, string unit, string tariff, decimal price, decimal stock) =>
            new() { Code = code, Name = name, GenericName = generic, Form = form, Strength = strength, Unit = unit, TariffCode = tariff, ItemType = StockItemType.Medication, Quantity = stock };
        var paracetamol = Medication("PARA500", "Paracetamol", "Paracetamol", "Tablet", "500mg", "tablet", "PARACETAMOL", 500m, 1000m);
        var amoxicillin = Medication("AMOX500", "Amoxicillin", "Amoxicillin", "Capsule", "500mg", "capsule", "AMOXICILLIN", 1_200m, 500m);
        var artemether = Medication("ART20", "Artemether/Lumefantrine", null, "Tablet", "20/120mg", "tablet", "ARTEMETHER", 1_500m, 300m);
        var cbc = new StockItem { Code = "CBC", Name = "Complete blood count", Category = "Laboratory", TariffCode = "CBC", ItemType = StockItemType.Test };
        var ultrasound = new StockItem { Code = "US", Name = "Ultrasound scan", Category = "Radiology", TariffCode = "ULTRASOUND", ItemType = StockItemType.Test };
        var appendectomy = new StockItem { Code = "APPEND", Name = "Appendectomy", TariffCode = "APPENDECTOMY", ItemType = StockItemType.Procedure };
        var consultation = new StockItem { Code = "CONSULT", Name = "Consultation", Category = "Appointment", Unit = "visit", ItemType = StockItemType.Service };
        var emergency = new StockItem { Code = "EMERGENCY", Name = "Emergency", Category = "Appointment", Unit = "visit", ItemType = StockItemType.Service };
        db.StockItems.AddRange(paracetamol, amoxicillin, artemether, cbc, ultrasound, appendectomy, consultation, emergency);

        // Private price rows plus one HMO price row per enrolled payer for every catalogue item.
        var privatePrices = new[]
        {
            (paracetamol, 500m), (amoxicillin, 1_200m), (artemether, 1_500m),
            (cbc, 5_000m), (ultrasound, 15_000m), (appendectomy, 250_000m),
            (consultation, 5_000m), (emergency, 20_000m)
        };
        foreach (var (item, price) in privatePrices)
            db.ItemPrices.Add(new ItemPrice { StockItemId = item.Id, PayerId = Payers.PrivateId, FundingSource = FundingSource.Private, UnitPrice = price, CoveragePercent = 0, EffectiveFrom = today });
        foreach (var item in privatePrices.Select(x => x.Item1))
            db.ItemPrices.Add(new ItemPrice
            {
                StockItemId = item.Id, PayerId = hmo.Id, FundingSource = FundingSource.Hmo, HmoCompanyId = hmo.Id,
                UnitPrice = privatePrices.Single(x => x.Item1 == item).Item2 * 0.9m,
                CoveragePercent = 90, EffectiveFrom = today
            });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed complete. Users: {Email} / {Password}", "admin@clinic.local", DefaultPassword);
    }

    private static FeeConfiguration Fee(FeeCode code, decimal amount, Guid adminId, DateOnly effectiveFrom) => new()
    {
        Code = code, Amount = amount, Currency = "NGN", EffectiveFrom = effectiveFrom, CreatedBy = adminId
    };

    /// <summary>
    /// Idempotently adds Consultation/Emergency catalogue items and a price row for Private plus
    /// every active HMO. This keeps existing databases usable after the appointment pricing change.
    /// </summary>
    private static async Task EnsureAppointmentReasonPricingAsync(HmsDbContext db, ILogger logger,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hmoIds = await db.HmoCompanies.AsNoTracking().Where(x => x.IsActive)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        var created = false;

        foreach (var (code, name, privatePrice) in new[]
                 {
                     ("CONSULT", AppointmentReasons.Consultation, 5_000m),
                     ("EMERGENCY", AppointmentReasons.Emergency, 20_000m)
                 })
        {
            var item = await db.StockItems.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (item is null)
            {
                item = new StockItem
                {
                    Code = code, Name = name, Category = "Appointment", Unit = "visit",
                    ItemType = StockItemType.Service
                };
                db.StockItems.Add(item);
                created = true;
            }

            if (!await db.ItemPrices.AnyAsync(x => x.StockItemId == item.Id && x.PayerId == Payers.PrivateId,
                    cancellationToken))
            {
                db.ItemPrices.Add(new ItemPrice
                {
                    StockItemId = item.Id, PayerId = Payers.PrivateId, FundingSource = FundingSource.Private,
                    UnitPrice = privatePrice, CoveragePercent = 0, EffectiveFrom = today
                });
                created = true;
            }

            foreach (var hmoId in hmoIds)
            {
                if (await db.ItemPrices.AnyAsync(x => x.StockItemId == item.Id && x.PayerId == hmoId,
                        cancellationToken)) continue;
                db.ItemPrices.Add(new ItemPrice
                {
                    StockItemId = item.Id, PayerId = hmoId, FundingSource = FundingSource.Hmo,
                    HmoCompanyId = hmoId, UnitPrice = Math.Round(privatePrice * 0.9m, 2),
                    CoveragePercent = 90, EffectiveFrom = today
                });
                created = true;
            }
        }

        if (!created) return;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ensured appointment reason catalogue pricing");
    }

    private static string ToEmailPrefix(StaffRole role) => role switch
    {
        StaffRole.HmoOffice => "hmo",
        StaffRole.HrOffice => "hr",
        _ => role.ToString().ToLowerInvariant()
    };
}
