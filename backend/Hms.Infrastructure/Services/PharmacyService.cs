using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Prescription lookup, stock issue and outpatient commit-to-Finance.</summary>
public sealed class PharmacyService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    IBillingService billing,
    ITariffPricingService tariffs) : IPharmacyService
{
    public async Task<IReadOnlyList<PharmacyQueueItemDto>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        return await db.Prescriptions.AsNoTracking()
            .Include(x => x.Encounter).ThenInclude(x => x!.Patient)
            .Where(x => x.Status == PrescriptionStatus.SentToPharmacy || x.Status == PrescriptionStatus.PartiallyDispensed)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new PharmacyQueueItemDto(x.EncounterId, x.Encounter!.EncounterNumber,
                (x.Encounter.Patient!.FirstName + " " + (x.Encounter.Patient.MiddleName ?? "") + " " + x.Encounter.Patient.LastName).Trim(), x.Encounter.Type == EncounterType.Inpatient,
                x.Status.ToString(), x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PharmacyPrescriptionDto> GetPrescriptionAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.AsNoTracking().Include(x => x.Patient).Include(x => x.Admission)
            .SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        var prescription = await db.Prescriptions.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.StockItem)
            .Where(x => x.EncounterId == encounterId && x.Status != PrescriptionStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppNotFoundException("No active prescription exists for this encounter.");

        Guid? hmoCompanyId = null;
        if (encounter.HmoEnrollmentId is not null)
        {
            hmoCompanyId = await db.PatientHmoEnrollments.AsNoTracking()
                .Where(x => x.Id == encounter.HmoEnrollmentId).Select(x => (Guid?)x.HmoCompanyId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var tariff = hmoCompanyId is null
            ? new Dictionary<string, HmoTariffItem>()
            : await tariffs.ResolveAsync(hmoCompanyId.Value, cancellationToken);
        var prices = await PrivatePricesAsync(
            prescription.Items.Select(x => x.StockItemId).Distinct().ToList(), cancellationToken);

        var items = prescription.Items.Select(item =>
        {
            tariff.TryGetValue(item.Medication?.TariffCode ?? string.Empty, out var tariffItem);
            return new PharmacyPrescriptionItemDto(
                item.Id, item.MedicationId, item.Medication?.Name ?? "Medication", item.Medication?.Strength,
                item.Medication?.Form, item.Medication?.Unit ?? "unit", item.Medication?.Quantity ?? 0m,
                item.QuantityPrescribed, item.QuantityDispensed, prices.GetValueOrDefault(item.StockItemId),
                item.Medication?.TariffCode, tariffItem?.UnitPrice, tariffItem?.CoveragePercent,
                encounter.Type == EncounterType.Inpatient);
        }).ToList();

        return new PharmacyPrescriptionDto(prescription.Id, encounter.Id, encounter.EncounterNumber,
            encounter.Patient?.FullName ?? "Unknown", encounter.Type == EncounterType.Inpatient,
            prescription.Status.ToString(), prescription.Notes, items,
            FinanceCommitBlockedReason(encounter) is null,
            FinanceCommitBlockedReason(encounter));
    }

    /// <summary>
    /// Compares the private market total with every HMO the patient belongs to. This is a read-only
    /// pricing view: switching the option never changes the encounter payer or the prescription.
    /// </summary>
    public async Task<PrescriptionPriceOptionsDto> GetPriceOptionsAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        var items = await db.Prescriptions.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.StockItem)
            .Where(x => x.EncounterId == encounterId && x.Status != PrescriptionStatus.Cancelled)
            .SelectMany(x => x.Items)
            .ToListAsync(cancellationToken);

        var prices = await PrivatePricesAsync(
            items.Select(x => x.StockItemId).Distinct().ToList(), cancellationToken);
        var marketTotal = items.Sum(x => x.QuantityPrescribed * prices.GetValueOrDefault(x.StockItemId));
        var options = new List<MedicationPriceOptionDto>
        {
            new("Private", null, "Private (market price)", marketTotal, 0m, marketTotal, false, [])
        };

        var enrollments = await db.PatientHmoEnrollments.AsNoTracking().Include(x => x.HmoCompany)
            .Where(x => x.PatientId == encounter.PatientId && x.IsActive)
            .OrderByDescending(x => x.IsPrimary)
            .ToListAsync(cancellationToken);

        foreach (var enrollment in enrollments.Where(x => x.HmoCompany is not null).GroupBy(x => x.HmoCompanyId).Select(g => g.First()))
        {
            var priceBook = await tariffs.ResolveAsync(enrollment.HmoCompanyId, cancellationToken);
            var total = 0m;
            var hmoCovered = 0m;
            var undercovered = false;
            var warnings = new List<string>();
            foreach (var item in items)
            {
                var medication = item.Medication;
                if (medication is null) continue;
                priceBook.TryGetValue(medication.TariffCode ?? string.Empty, out var tariffItem);
                var marketPrice = prices.GetValueOrDefault(item.StockItemId);
                var unitPrice = tariffItem?.UnitPrice ?? marketPrice;
                var amount = Math.Round(item.QuantityPrescribed * unitPrice, 2, MidpointRounding.AwayFromZero);
                total += amount;
                if (tariffItem is null)
                {
                    warnings.Add($"No active tariff for {medication.Name}; market price used.");
                    continue;
                }
                var percent = Math.Clamp(tariffItem.CoveragePercent, 0m, 100m);
                hmoCovered += Math.Round(amount * percent / 100m, 2, MidpointRounding.AwayFromZero);
                if (tariffItem.UnitPrice < marketPrice)
                {
                    undercovered = true;
                    warnings.Add($"{medication.Name}: negotiated {tariffItem.UnitPrice:N2} is below market {marketPrice:N2}; the difference is not HMO-covered.");
                }
            }
            options.Add(new MedicationPriceOptionDto("Hmo", enrollment.HmoCompanyId,
                enrollment.HmoCompany!.Name, total, hmoCovered, total - hmoCovered, undercovered, warnings));
        }

        return new PrescriptionPriceOptionsDto(encounterId, marketTotal, options);
    }

    /// <summary>Dispenses available quantities. Outpatient bills commit immediately; inpatient charges wait for Nurse administration.</summary>
    public async Task<DispenseResultDto> DispenseAsync(DispenseRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0) throw new AppConflictException("Select at least one medication to dispense.");
        if (request.Items.Select(x => x.PrescriptionItemId).Distinct().Count() != request.Items.Count)
            throw new AppConflictException("Duplicate prescription items are not allowed.");

        var prescription = await db.Prescriptions
            .Include(x => x.Items).ThenInclude(x => x.StockItem)
            .Include(x => x.Encounter).ThenInclude(x => x!.Patient)
            .Include(x => x.Encounter).ThenInclude(x => x!.Admission)
            .SingleOrDefaultAsync(x => x.Id == request.PrescriptionId, cancellationToken)
            ?? throw new AppNotFoundException("Prescription not found.");
        if (prescription.Status is PrescriptionStatus.Cancelled or PrescriptionStatus.Dispensed)
            throw new AppConflictException("This prescription cannot be dispensed.");
        var encounter = prescription.Encounter!;
        var isInpatient = encounter.Type == EncounterType.Inpatient;

        // An outpatient encounter signed with disposition "Admit" is treated as inpatient for
        // routing: the prescription is dispensed to ward and charges accrue on the admission
        // bill through nurse medication logs, not committed to Finance at dispense time.
        var isWardDispense = isInpatient || encounter.Disposition == EncounterDisposition.Admit;

        // Outpatient prescriptions may only be committed after the doctor has signed the encounter.
        // Inpatient ward dispensing is still allowed while admitted; those charges accrue on the
        // admission bill and are only committed to Finance at discharge.
        if (!isWardDispense && FinanceCommitBlockedReason(encounter) is { } blocked)
            throw new AppConflictException(blocked);

        var warnings = new List<string>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var dispense = new Dispense
        {
            PrescriptionId = prescription.Id,
            EncounterId = encounter.Id,
            PharmacyUserId = user.RequireId(),
            Status = DispenseStatus.Dispensed,
            DispensedAt = DateTimeOffset.UtcNow,
            CreatedBy = user.RequireId()
        };

        var bill = await billing.GetOrCreateVisitBillAsync(encounter, cancellationToken);

        Guid? hmoCompanyId = null;
        if (encounter.HmoEnrollmentId is not null)
            hmoCompanyId = await db.PatientHmoEnrollments.AsNoTracking().Where(x => x.Id == encounter.HmoEnrollmentId)
                .Select(x => (Guid?)x.HmoCompanyId).FirstOrDefaultAsync(cancellationToken);
        var tariff = hmoCompanyId is null ? new Dictionary<string, HmoTariffItem>() : await tariffs.ResolveAsync(hmoCompanyId.Value, cancellationToken);
        var medIds = prescription.Items.Select(x => x.StockItemId).Distinct().ToList();
        var privatePrices = await PrivatePricesAsync(medIds, cancellationToken);
        var allTariffsResolved = true;

        foreach (var requestItem in request.Items)
        {
            var item = prescription.Items.SingleOrDefault(x => x.Id == requestItem.PrescriptionItemId)
                ?? throw new AppConflictException("A prescription item does not belong to this prescription.");
            if (requestItem.Quantity <= 0) throw new AppConflictException("Dispense quantity must be greater than zero.");
            var remaining = item.QuantityPrescribed - item.QuantityDispensed;
            if (requestItem.Quantity > remaining)
                throw new AppConflictException($"Quantity for {item.Medication?.Name} exceeds the remaining prescription.");
            var medication = item.Medication ?? throw new AppConflictException("Medication not found.");
            var available = medication.Quantity;
            if (available < requestItem.Quantity)
                throw new AppConflictException($"Insufficient stock for {medication.Name}. Available: {available} {medication.Unit}.");

            HmoTariffItem? tariffItem = null;
            if (hmoCompanyId is not null && !string.IsNullOrWhiteSpace(medication.TariffCode))
                tariff.TryGetValue(medication.TariffCode, out tariffItem);
            if (hmoCompanyId is not null && tariffItem is null) allTariffsResolved = false;

            var dispenseItem = new DispenseItem
            {
                DispenseId = dispense.Id,
                PrescriptionItemId = item.Id,
                MedicationId = medication.Id,
                Quantity = requestItem.Quantity,
                UnitPrice = tariffItem?.UnitPrice ?? privatePrices.GetValueOrDefault(medication.Id),
                TariffItemId = tariffItem?.Id
            };
            dispense.Items.Add(dispenseItem);
            item.QuantityDispensed += requestItem.Quantity;
            medication.Quantity -= requestItem.Quantity;
            medication.UpdatedAt = DateTimeOffset.UtcNow;
            db.StockMovements.Add(new StockMovement
            {
                MedicationId = medication.Id,
                MovementType = StockMovementType.Dispense,
                QuantityDelta = -requestItem.Quantity,
                UnitCost = dispenseItem.UnitPrice,
                ReferenceType = nameof(Dispense),
                ReferenceId = dispense.Id,
                CreatedBy = user.RequireId()
            });

            if (isWardDispense) continue;

            var line = await billing.AddChargeAsync(bill.Id, BillLineCategory.Medication, medication.Name,
                requestItem.Quantity, dispenseItem.UnitPrice, ChargeSourceType.MedicationDispenseItem,
                dispenseItem.Id, $"med-dispense:{dispenseItem.Id}", encounter.Id, null,
                medication.TariffCode, cancellationToken);
            dispenseItem.HmoAmount = line.HmoAmount;
            dispenseItem.PatientAmount = line.PatientAmount;
            if (line.TariffItemId is not null)
            {
                line.TariffConfirmedAt = DateTimeOffset.UtcNow;
                line.TariffConfirmedBy = user.RequireId();
            }
            else if (hmoCompanyId is not null)
            {
                warnings.Add($"No active HMO tariff matched {medication.Name} ({medication.TariffCode}). HMO Office must review the line.");
            }
        }

        var fullyDispensed = prescription.Items.All(x => x.QuantityDispensed >= x.QuantityPrescribed);
        prescription.Status = fullyDispensed ? PrescriptionStatus.Dispensed : PrescriptionStatus.PartiallyDispensed;
        if (allTariffsResolved)
        {
            dispense.TariffConfirmedAt = DateTimeOffset.UtcNow;
            dispense.TariffConfirmedBy = user.RequireId();
        }

        db.Dispenses.Add(dispense);
        await db.SaveChangesAsync(cancellationToken);

        if (!isInpatient)
        {
            bill = await billing.CommitAsync(bill.Id, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await audit.WriteAsync("medication.dispensed", nameof(Dispense), dispense.Id.ToString(), encounter.PatientId, encounter.Id, bill.Id,
            after: new { PrescriptionStatus = prescription.Status, Items = request.Items.Count, BillStatus = bill.Status }, cancellationToken: cancellationToken);

        return new DispenseResultDto(dispense.Id, bill.Id, bill.Status.ToString(),
            bill.Status is not (BillStatus.Draft or BillStatus.Void),
            bill.PatientPayableTotal, bill.HmoPayableTotal, warnings);
    }

    /// <summary>
    /// Finance commit eligibility. Outpatient encounters must be signed first; inpatient charges
    /// stay on the draft admission bill until the patient is discharged.
    /// </summary>
    private static string? FinanceCommitBlockedReason(Encounter encounter)
    {
        if (encounter.Type == EncounterType.Outpatient)
        {
            if (encounter.Status == EncounterStatus.Completed) return null;
            return encounter.Status == EncounterStatus.Cancelled
                ? "Encounter was cancelled and cannot be committed to Finance."
                : "Encounter is still in draft. The doctor must sign the encounter before it can be committed to Finance.";
        }

        var admission = encounter.Admission;
        if (admission is null || admission.Status != AdmissionStatus.Discharged)
            return "Patient has not yet been discharged. The admission bill can only be committed to Finance after discharge.";

        return null;
    }

    public async Task ConfirmTariffAsync(Guid billId, ConfirmTariffRequest request, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == billId, cancellationToken)
            ?? throw new AppNotFoundException("Bill not found.");
        if (bill.PayerType != PayerType.Hmo) throw new AppConflictException("Tariff confirmation applies only to HMO bills.");
        foreach (var lineId in request.BillLineIds.Distinct())
        {
            var line = bill.Lines.SingleOrDefault(x => x.Id == lineId)
                ?? throw new AppConflictException("A bill line does not belong to this bill.");
            line.TariffConfirmedAt = DateTimeOffset.UtcNow;
            line.TariffConfirmedBy = user.RequireId();
        }
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.tariff-confirmed", nameof(Bill), bill.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id, cancellationToken: cancellationToken);
    }

    public async Task<PharmacyPrescriptionDto> AdjustPrescriptionQuantityAsync(Guid prescriptionId, Guid itemId,
        decimal quantityPrescribed, CancellationToken cancellationToken = default)
    {
        if (quantityPrescribed <= 0) throw new AppConflictException("Prescribed quantity must be greater than zero.");
        var prescription = await db.Prescriptions.Include(x => x.Items).Include(x => x.Encounter)
            .SingleOrDefaultAsync(x => x.Id == prescriptionId, cancellationToken)
            ?? throw new AppNotFoundException("Prescription not found.");
        if (prescription.Status == PrescriptionStatus.Cancelled)
            throw new AppConflictException("A cancelled prescription cannot be adjusted.");
        var item = prescription.Items.SingleOrDefault(x => x.Id == itemId)
            ?? throw new AppNotFoundException("Prescription item not found.");
        if (quantityPrescribed < item.QuantityDispensed)
            throw new AppConflictException("Prescribed quantity cannot be less than the quantity already dispensed.");
        item.QuantityPrescribed = quantityPrescribed;
        prescription.Status = item.QuantityDispensed >= quantityPrescribed
            ? PrescriptionStatus.Dispensed
            : item.QuantityDispensed > 0 ? PrescriptionStatus.PartiallyDispensed : PrescriptionStatus.SentToPharmacy;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("prescription.quantity-adjusted", nameof(PrescriptionItem), item.Id.ToString(),
            prescription.Encounter.PatientId, prescription.EncounterId,
            after: new { item.QuantityPrescribed, item.QuantityDispensed }, cancellationToken: cancellationToken);
        return await GetPrescriptionAsync(prescription.EncounterId, cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal>> PrivatePricesAsync(
        IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0) return [];
        return await db.ItemPrices.AsNoTracking()
            .Where(x => x.FundingSource == FundingSource.Private && x.IsActive && itemIds.Contains(x.StockItemId))
            .ToDictionaryAsync(x => x.StockItemId, x => x.UnitPrice, cancellationToken);
    }
}
