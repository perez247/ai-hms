using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Vitals, prescriptions, investigations, surgery reports and nurse administration.</summary>
public sealed partial class ClinicalService
{

    public async Task<VitalDto> RecordVitalsAsync(RecordVitalsRequest request, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.SingleOrDefaultAsync(x => x.Id == request.EncounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        if (encounter.Status != EncounterStatus.Open) throw new AppConflictException("Vitals cannot be recorded on a closed encounter.");
        if (request.TemperatureC is < 25m or > 45m) throw new AppConflictException("Temperature is outside a plausible range.");
        if (request.PainScore is < 0 or > 10) throw new AppConflictException("Pain score must be between 0 and 10.");

        var vital = new Vital
        {
            EncounterId = encounter.Id,
            NurseId = user.RequireId(),
            TemperatureC = request.TemperatureC,
            Systolic = request.Systolic,
            Diastolic = request.Diastolic,
            Pulse = request.Pulse,
            RespiratoryRate = request.RespiratoryRate,
            Spo2 = request.Spo2,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            PainScore = request.PainScore,
            Notes = request.Notes,
            CreatedBy = user.RequireId()
        };
        db.Vitals.Add(vital);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("vitals.recorded", nameof(Vital), vital.Id.ToString(), encounter.PatientId, encounter.Id, cancellationToken: cancellationToken);
        return MapVital(vital);
    }

    public async Task<PrescriptionSummaryDto> CreatePrescriptionAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.SingleOrDefaultAsync(x => x.Id == request.EncounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        if (encounter.Status != EncounterStatus.Open) throw new AppConflictException("Prescriptions require an open encounter.");
        if (request.Items.Count == 0) throw new AppConflictException("At least one medication is required.");
        var medicationIds = request.Items.Select(x => x.MedicationId).Distinct().ToList();
        var medications = await db.Medications.Where(x => medicationIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (medications.Count != medicationIds.Count) throw new AppConflictException("One or more medications are not active.");

        var prescription = new Prescription
        {
            EncounterId = encounter.Id,
            DoctorId = user.RequireId(),
            Status = PrescriptionStatus.SentToPharmacy,
            Notes = request.Notes,
            SentToPharmacyAt = DateTimeOffset.UtcNow,
            CreatedBy = user.RequireId()
        };
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0) throw new AppConflictException("Medication quantity must be greater than zero.");
            prescription.Items.Add(new PrescriptionItem
            {
                MedicationId = item.MedicationId,
                Dose = item.Dose,
                Route = item.Route,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                QuantityPrescribed = item.Quantity,
                IsPrn = item.IsPrn,
                Instructions = item.Instructions
            });
        }
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("prescription.created", nameof(Prescription), prescription.Id.ToString(), encounter.PatientId, encounter.Id, cancellationToken: cancellationToken);
        return new PrescriptionSummaryDto(prescription.Id, prescription.Status, prescription.CreatedAt, prescription.Items.Count);
    }

    public async Task<InvestigationSummaryDto> CreateInvestigationAsync(CreateInvestigationRequest request, CancellationToken cancellationToken = default)
    {
        var encounter = await RequireOpenEncounterAsync(request.EncounterId, cancellationToken);
        var catalog = await db.InvestigationCatalogItems.SingleOrDefaultAsync(x => x.Id == request.CatalogItemId && x.IsActive, cancellationToken)
            ?? throw new AppNotFoundException("Investigation catalog item not found.");
        var order = new InvestigationOrder
        {
            EncounterId = encounter.Id,
            DoctorId = user.RequireId(),
            CatalogItemId = catalog.Id,
            Status = InvestigationStatus.Ordered,
            CreatedBy = user.RequireId()
        };
        db.InvestigationOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("investigation.ordered", nameof(InvestigationOrder), order.Id.ToString(), encounter.PatientId, encounter.Id, cancellationToken: cancellationToken);
        return new InvestigationSummaryDto(order.Id, catalog.Name, order.Status, order.OrderedAt);
    }

    public async Task<InvestigationSummaryDto> CompleteInvestigationAsync(Guid investigationId, CompleteInvestigationRequest request, CancellationToken cancellationToken = default)
    {
        var order = await db.InvestigationOrders.Include(x => x.CatalogItem).Include(x => x.Encounter)
            .SingleOrDefaultAsync(x => x.Id == investigationId, cancellationToken)
            ?? throw new AppNotFoundException("Investigation not found.");
        if (order.Status == InvestigationStatus.Completed) throw new AppConflictException("Investigation is already completed.");
        if (order.Status == InvestigationStatus.Cancelled) throw new AppConflictException("Cancelled investigations cannot be completed.");
        if (string.IsNullOrWhiteSpace(request.ReportText)) throw new AppConflictException("A report is required.");

        order.Status = InvestigationStatus.Completed;
        order.PerformedAt = DateTimeOffset.UtcNow;
        order.PerformedBy = user.RequireId();
        order.ReportText = request.ReportText;
        order.ReportFileRef = request.ReportFileRef;
        order.ReferralDestination = request.ReferralDestination;
        await AddParticipantsAsync(request.ParticipantUserIds, order, cancellationToken);

        var bill = order.Encounter!.AdmissionId is null
            ? await billing.GetOrCreateEncounterBillAsync(order.Encounter, cancellationToken)
            : await billing.GetOrCreateAdmissionBillAsync(await db.Admissions.SingleAsync(x => x.Id == order.Encounter.AdmissionId, cancellationToken), cancellationToken);
        await billing.AddChargeAsync(bill.Id, BillLineCategory.Investigation, order.CatalogItem!.Name, 1m, order.CatalogItem.Price,
            ChargeSourceType.InvestigationOrder, order.Id, $"investigation:{order.Id}", order.EncounterId,
            order.Encounter.AdmissionId, order.CatalogItem.TariffCode, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("investigation.completed", nameof(InvestigationOrder), order.Id.ToString(), order.Encounter.PatientId, order.EncounterId, bill.Id, cancellationToken: cancellationToken);
        return new InvestigationSummaryDto(order.Id, order.CatalogItem.Name, order.Status, order.OrderedAt);
    }

    public async Task<SurgerySummaryDto> CreateSurgeryAsync(CreateSurgeryRequest request, CancellationToken cancellationToken = default)
    {
        var encounter = await RequireOpenEncounterAsync(request.EncounterId, cancellationToken);
        var catalog = await db.SurgeryCatalogItems.SingleOrDefaultAsync(x => x.Id == request.CatalogItemId && x.IsActive, cancellationToken)
            ?? throw new AppNotFoundException("Surgery catalog item not found.");
        var surgery = new SurgeryCase
        {
            EncounterId = encounter.Id,
            LeadSurgeonId = user.RequireId(),
            CatalogItemId = catalog.Id,
            Status = SurgeryStatus.Scheduled,
            ScheduledAt = request.ScheduledAt,
            Theater = request.Theater,
            CreatedBy = user.RequireId()
        };
        db.SurgeryCases.Add(surgery);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("surgery.scheduled", nameof(SurgeryCase), surgery.Id.ToString(), encounter.PatientId, encounter.Id, cancellationToken: cancellationToken);
        return new SurgerySummaryDto(surgery.Id, catalog.Name, surgery.Status, surgery.ScheduledAt);
    }

    public async Task<SurgerySummaryDto> CompleteSurgeryAsync(Guid surgeryId, CompleteSurgeryRequest request, CancellationToken cancellationToken = default)
    {
        var surgery = await db.SurgeryCases.Include(x => x.CatalogItem).Include(x => x.Encounter)
            .SingleOrDefaultAsync(x => x.Id == surgeryId, cancellationToken)
            ?? throw new AppNotFoundException("Surgery not found.");
        if (surgery.Status == SurgeryStatus.Completed) throw new AppConflictException("Surgery is already completed.");
        if (surgery.Status == SurgeryStatus.Cancelled) throw new AppConflictException("Cancelled surgery cannot be completed.");
        if (string.IsNullOrWhiteSpace(request.ReportText)) throw new AppConflictException("An operative report is required.");

        surgery.Status = SurgeryStatus.Completed;
        surgery.PerformedAt = DateTimeOffset.UtcNow;
        surgery.ReportText = request.ReportText;
        surgery.ReportFileRef = request.ReportFileRef;
        await AddParticipantsAsync(request.ParticipantUserIds, surgery, cancellationToken);

        var bill = surgery.Encounter!.AdmissionId is null
            ? await billing.GetOrCreateEncounterBillAsync(surgery.Encounter, cancellationToken)
            : await billing.GetOrCreateAdmissionBillAsync(await db.Admissions.SingleAsync(x => x.Id == surgery.Encounter.AdmissionId, cancellationToken), cancellationToken);
        await billing.AddChargeAsync(bill.Id, BillLineCategory.Surgery, surgery.CatalogItem!.Name, 1m, surgery.CatalogItem.Price,
            ChargeSourceType.SurgeryCase, surgery.Id, $"surgery:{surgery.Id}", surgery.EncounterId,
            surgery.Encounter.AdmissionId, surgery.CatalogItem.TariffCode, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("surgery.completed", nameof(SurgeryCase), surgery.Id.ToString(), surgery.Encounter.PatientId, surgery.EncounterId, bill.Id, cancellationToken: cancellationToken);
        return new SurgerySummaryDto(surgery.Id, surgery.CatalogItem.Name, surgery.Status, surgery.ScheduledAt);
    }

    public async Task<Guid> RecordAdministrationAsync(RecordAdministrationRequest request, CancellationToken cancellationToken = default)
    {
        var dispenseItem = await db.DispenseItems
            .Include(x => x.Dispense).ThenInclude(x => x!.Encounter)
            .Include(x => x.PrescriptionItem)
            .Include(x => x.Medication)
            .SingleOrDefaultAsync(x => x.Id == request.DispenseItemId, cancellationToken)
            ?? throw new AppNotFoundException("Dispense item not found.");
        var encounter = dispenseItem.Dispense!.Encounter!;
        if (request.DoseGiven <= 0) throw new AppConflictException("Dose must be greater than zero.");
        var administration = new MedicationAdministration
        {
            EncounterId = encounter.Id,
            PrescriptionItemId = dispenseItem.PrescriptionItemId,
            DispenseItemId = dispenseItem.Id,
            NurseId = user.RequireId(),
            DoseGiven = request.DoseGiven,
            Route = request.Route,
            BatchNumber = request.BatchNumber,
            Notes = request.Notes,
            CreatedBy = user.RequireId()
        };
        db.MedicationAdministrations.Add(administration);
        await db.SaveChangesAsync(cancellationToken);

        if (encounter.AdmissionId is not null)
        {
            var admission = await db.Admissions.SingleAsync(x => x.Id == encounter.AdmissionId, cancellationToken);
            var bill = await billing.GetOrCreateAdmissionBillAsync(admission, cancellationToken);
            await billing.AddChargeAsync(bill.Id, BillLineCategory.Medication,
                dispenseItem.Medication?.Name ?? "Medication", request.DoseGiven, dispenseItem.UnitPrice,
                ChargeSourceType.MedicationAdministration, administration.Id, $"med-admin:{administration.Id}",
                encounter.Id, admission.Id, dispenseItem.Medication?.TariffCode, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("medication.administered", nameof(MedicationAdministration), administration.Id.ToString(), encounter.PatientId, encounter.Id, cancellationToken: cancellationToken);
        return administration.Id;
    }
}
