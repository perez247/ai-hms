using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hms.Infrastructure.Services;

/// <summary>Outpatient/inpatient encounter creation and reads.</summary>
public sealed partial class ClinicalService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    INumberGenerator numbers,
    IBillingService billing,
    IFundingService funding) : IClinicalService
{

    public async Task<EncounterDto> CreateOutpatientEncounterAsync(CreateOutpatientEncounterRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        await EnsureDoctorAsync(request.DoctorId, cancellationToken);
        await EnsureEnrollmentAsync(patient.Id, request.HmoEnrollmentId, cancellationToken);
        Appointment? appointment = null;
        if (request.AppointmentId is not null)
        {
            appointment = await db.Appointments.SingleOrDefaultAsync(x => x.Id == request.AppointmentId && x.PatientId == patient.Id, cancellationToken)
                ?? throw new AppNotFoundException("Appointment not found for this patient.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var serviceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var fundingContext = await funding.VerifyAsync(
            appointment?.FundingSource ?? (request.HmoEnrollmentId is null ? FundingSource.Private : FundingSource.Hmo),
            appointment?.HmoEnrollmentId ?? request.HmoEnrollmentId, patient.Id, serviceDate, cancellationToken);
        var encounter = new Encounter
        {
            EncounterNumber = await numbers.NextAsync("encounter_number_seq", "ENC", cancellationToken),
            PatientId = patient.Id,
            DoctorId = request.DoctorId,
            AppointmentId = appointment?.Id,
            Type = EncounterType.Outpatient,
            IsEmergency = request.IsEmergency,
            FundingSource = fundingContext.Source,
            HmoEnrollmentId = fundingContext.HmoEnrollmentId,
            PresentingComplaint = request.PresentingComplaint,
            CreatedBy = user.RequireId()
        };
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);
        await billing.GetOrCreateVisitBillAsync(encounter, cancellationToken);

        if (request.IsEmergency)
        {
            await billing.CreateAdministrativeBillAsync(patient.Id, BillKind.Emergency, FeeCode.Emergency,
                encounter.Id, ChargeSourceType.Encounter, $"Emergency fee {encounter.EncounterNumber}", cancellationToken);
        }

        if (appointment is not null)
        {
            appointment.Status = AppointmentStatus.CheckedIn;
            appointment.CheckedInAt ??= DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await audit.WriteAsync("encounter.outpatient-created", nameof(Encounter), encounter.Id.ToString(), patient.Id, encounter.Id,
            after: new { encounter.EncounterNumber, encounter.IsEmergency }, cancellationToken: cancellationToken);
        return await GetEncounterAsync(encounter.Id, cancellationToken);
    }

    public async Task<AdmissionDto> CreateAdmissionAsync(CreateAdmissionRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        await EnsureDoctorAsync(request.DoctorId, cancellationToken);
        await EnsureEnrollmentAsync(patient.Id, request.HmoEnrollmentId, cancellationToken);
        var hasActiveAdmission = await db.Admissions.AnyAsync(x => x.PatientId == patient.Id
            && (x.Status == AdmissionStatus.Active || x.Status == AdmissionStatus.PendingDischarge), cancellationToken);
        if (hasActiveAdmission) throw new AppConflictException("Patient already has an active admission.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var admission = new Admission
        {
            AdmissionNumber = await numbers.NextAsync("admission_number_seq", "ADM", cancellationToken),
            PatientId = patient.Id,
            AttendingDoctorId = request.DoctorId,
            HmoEnrollmentId = request.HmoEnrollmentId,
            WardType = request.WardType,
            BedNumber = request.BedNumber,
            AdmittedAt = request.AdmittedAt ?? DateTimeOffset.UtcNow,
            CreatedBy = user.RequireId()
        };
        db.Admissions.Add(admission);
        await db.SaveChangesAsync(cancellationToken);

        var encounter = new Encounter
        {
            EncounterNumber = await numbers.NextAsync("encounter_number_seq", "ENC", cancellationToken),
            PatientId = patient.Id,
            DoctorId = request.DoctorId,
            AdmissionId = admission.Id,
            Type = EncounterType.Inpatient,
            FundingSource = request.HmoEnrollmentId is null ? FundingSource.Private : FundingSource.Hmo,
            HmoEnrollmentId = request.HmoEnrollmentId,
            PresentingComplaint = request.PresentingComplaint,
            CreatedBy = user.RequireId()
        };
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);
        await billing.GetOrCreateVisitBillAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await audit.WriteAsync("admission.created", nameof(Admission), admission.Id.ToString(), patient.Id, encounter.Id,
            after: new { admission.AdmissionNumber, admission.WardType }, cancellationToken: cancellationToken);
        return await GetAdmissionAsync(admission.Id, cancellationToken);
    }

    public async Task<EncounterDto> AddInpatientEncounterAsync(CreateInpatientEncounterRequest request, CancellationToken cancellationToken = default)
    {
        var admission = await db.Admissions.Include(x => x.Patient)
            .SingleOrDefaultAsync(x => x.Id == request.AdmissionId, cancellationToken)
            ?? throw new AppNotFoundException("Admission not found.");
        if (admission.Status != AdmissionStatus.Active)
            throw new AppConflictException("Only an active admission can receive new encounter entries.");
        await EnsureDoctorAsync(request.DoctorId, cancellationToken);

        var encounter = new Encounter
        {
            EncounterNumber = await numbers.NextAsync("encounter_number_seq", "ENC", cancellationToken),
            PatientId = admission.PatientId,
            DoctorId = request.DoctorId,
            AdmissionId = admission.Id,
            Type = EncounterType.Inpatient,
            HmoEnrollmentId = admission.HmoEnrollmentId,
            PresentingComplaint = request.PresentingComplaint,
            HistoryOfPresentingComplaint = request.HistoryOfPresentingComplaint,
            PastMedicalSurgicalHistory = request.PastMedicalSurgicalHistory,
            FamilyHistory = request.FamilyHistory,
            Diagnosis = request.Diagnosis,
            ClerkingDetails = request.ClerkingDetails,
            CreatedBy = user.RequireId()
        };
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);

        // Copy forward medications from the prior encounter's plan into the new encounter.
        if (request.Medications is not null && request.Medications.Count > 0)
        {
            var prescription = new Prescription
            {
                EncounterId = encounter.Id,
                DoctorId = request.DoctorId,
                Status = PrescriptionStatus.SentToPharmacy,
                SentToPharmacyAt = DateTimeOffset.UtcNow,
                CreatedBy = user.RequireId(),
            };
            db.Prescriptions.Add(prescription);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var row in request.Medications)
            {
                var stockItem = await db.StockItems.SingleOrDefaultAsync(x => x.Id == row.StockItemId, cancellationToken)
                    ?? throw new AppNotFoundException("Medication not found.");
                if (stockItem.ItemType != StockItemType.Medication || !stockItem.IsActive)
                    throw new AppConflictException($"{stockItem.Name} is not an active medication.");
                db.PrescriptionItems.Add(new PrescriptionItem
                {
                    PrescriptionId = prescription.Id,
                    StockItemId = row.StockItemId,
                    Dose = row.Dose.Trim(),
                    Route = row.Route,
                    Frequency = row.Frequency,
                    DurationDays = row.DurationDays,
                    QuantityPrescribed = row.Quantity,
                    QuantityDispensed = 0,
                    Instructions = string.IsNullOrWhiteSpace(row.Instructions) ? null : row.Instructions.Trim(),
                });
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        await billing.GetOrCreateVisitBillAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("encounter.inpatient-created", nameof(Encounter), encounter.Id.ToString(), admission.PatientId, encounter.Id, cancellationToken: cancellationToken);
        return await GetEncounterAsync(encounter.Id, cancellationToken);
    }

    public async Task<EncounterDto> GetEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters
            // Vitals are a patient concern: load the patient's readings across all encounters.
            .Include(x => x.Patient).ThenInclude(p => p!.Vitals)
            .Include(x => x.Prescriptions).ThenInclude(p => p.Items).ThenInclude(i => i.StockItem)
            .Include(x => x.Investigations).ThenInclude(x => x.CatalogItem)
            .Include(x => x.Surgeries).ThenInclude(x => x.CatalogItem)
            .Include(x => x.ServiceRequests).ThenInclude(x => x.StockItem)
            .SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        var priorRows = await db.Encounters.AsNoTracking()
            .Where(x => x.PatientId == encounter.PatientId && x.Id != encounter.Id && x.StartedAt < encounter.StartedAt)
            .OrderByDescending(x => x.StartedAt)
            .Take(50)
            .Select(x => new
            {
                x.Id,
                x.EncounterNumber,
                x.Type,
                x.Status,
                x.DoctorId,
                x.StartedAt,
                x.PresentingComplaint,
                x.Diagnosis,
                x.Disposition,
                x.PastMedicalSurgicalHistory,
                x.FamilyHistory,
                x.PresentingComplaintDurationDays,
                x.HistoryOfPresentingComplaint,
                x.Examination,
                x.ClerkingDetails
            })
            .ToListAsync(cancellationToken);
        var priorIds = priorRows.Select(x => x.Id).ToList();
        var priorMedications = await db.Prescriptions.AsNoTracking()
            .Where(x => priorIds.Contains(x.EncounterId) && x.Status != PrescriptionStatus.Cancelled)
            .Include(x => x.Items).ThenInclude(i => i.StockItem)
            .ToListAsync(cancellationToken);
        var priorRequests = await db.ServiceRequests.AsNoTracking()
            .Where(x => priorIds.Contains(x.EncounterId))
            .Include(x => x.StockItem)
            .ToListAsync(cancellationToken);
        var priorRequestIds = priorRequests.Select(x => x.Id).ToList();
        var priorResultRequestIds = await db.ServiceResults.AsNoTracking()
            .Where(x => priorRequestIds.Contains(x.RequestId) && x.Status != ServiceResultStatus.Pending)
            .Select(x => x.RequestId)
            .ToListAsync(cancellationToken);
        var priorResultSet = priorResultRequestIds.ToHashSet();
        var doctorIds = priorRows.Select(x => x.DoctorId).Append(encounter.DoctorId).Distinct().ToList();
        var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        var doctorName = doctorNames.GetValueOrDefault(encounter.DoctorId, "Unknown");
        var billId = await db.Bills.AsNoTracking()
            .Where(x => x.Status != BillStatus.Void && x.Kind == BillKind.Visit
                && (x.AppointmentId == encounter.AppointmentId || x.EncounterId == encounterId))
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        var serviceRequestIds = encounter.ServiceRequests.Select(x => x.Id).ToList();
        var results = await db.ServiceResults.AsNoTracking().Where(x => serviceRequestIds.Contains(x.RequestId))
            .ToDictionaryAsync(x => x.RequestId, cancellationToken);
        var priorEncounters = priorRows.Select(x =>
        {
            var medications = priorMedications.Where(p => p.EncounterId == x.Id)
                .SelectMany(p => p.Items.Select(item => new PrescriptionItemSummaryDto(
                    item.Id, item.StockItem?.Name ?? "Medication", item.Dose, item.Route, item.Frequency,
                    item.DurationDays, item.QuantityPrescribed, item.QuantityDispensed, item.Instructions,
                    item.StockItemId)))
                .ToList();
            var orders = priorRequests
                .Where(r => r.EncounterId == x.Id && r.Status != ServiceRequestStatus.Cancelled)
                .Select(r => new EncounterPlanOrderDto(r.Id, r.StockItem?.Name ?? "Item", r.Kind, r.Status,
                    r.Priority, r.ClinicalNote))
                .ToList();
            var resultCount = priorRequests.Count(r => r.EncounterId == x.Id && priorResultSet.Contains(r.Id));
            return new EncounterHistoryItemDto(
                x.Id, x.EncounterNumber, x.Type, x.Status, doctorNames.GetValueOrDefault(x.DoctorId, "Unknown"),
                x.StartedAt, x.PresentingComplaint, x.Diagnosis, x.Disposition?.ToString(),
                x.PastMedicalSurgicalHistory, x.FamilyHistory, x.PresentingComplaintDurationDays,
                x.HistoryOfPresentingComplaint, ParseExamination(x.Examination),
                ParseClerkingDetails(x.ClerkingDetails), medications, orders, resultCount);
        }).ToList();
        return new EncounterDto(
            encounter.Id, encounter.EncounterNumber, encounter.PatientId,
            encounter.Patient?.PatientNumber ?? string.Empty, encounter.Patient?.FullName ?? "Unknown",
            encounter.DoctorId, doctorName, encounter.Type, encounter.Status,
            encounter.IsEmergency, encounter.StartedAt, encounter.ClosedAt, encounter.AdmissionId, billId,
            encounter.PresentingComplaint, encounter.HistoryOfPresentingComplaint, encounter.PastMedicalSurgicalHistory,
            encounter.FamilyHistory, ParseExamination(encounter.Examination), encounter.Diagnosis, encounter.Disposition?.ToString(),
            // The details view shows only the five most recent readings, newest first.
            encounter.Patient?.Vitals.OrderByDescending(v => v.RecordedAt).Take(5).Select(MapVital).ToList() ?? [],
            encounter.Prescriptions.OrderByDescending(x => x.CreatedAt).Select(x => new PrescriptionSummaryDto(
                x.Id, x.Status, x.CreatedAt, x.Items.Count,
                x.Items.Select(item => new PrescriptionItemSummaryDto(item.Id, item.StockItem?.Name ?? "Medication",
                    item.Dose, item.Route, item.Frequency, item.DurationDays,
                    item.QuantityPrescribed, item.QuantityDispensed, item.Instructions, item.StockItemId)).ToList())).ToList(),
            encounter.Investigations.Select(x => new InvestigationSummaryDto(x.Id, x.CatalogItem?.Name ?? "Investigation", x.Status, x.OrderedAt)).ToList(),
            encounter.Surgeries.Select(x => new SurgerySummaryDto(x.Id, x.CatalogItem?.Name ?? "Surgery", x.Status, x.ScheduledAt)).ToList(),
            encounter.FundingSource, encounter.HmoEnrollmentId,
            encounter.ServiceRequests.Select(x => new ServiceRequestSummaryDto(x.Id, x.RequestNumber, x.Kind,
                x.StockItem?.Name ?? "Item", x.Status, results.GetValueOrDefault(x.Id)?.Status,
                results.GetValueOrDefault(x.Id)?.ResultText, results.GetValueOrDefault(x.Id)?.Findings,
                x.UnitPriceSnapshot, x.FundingSource, results.GetValueOrDefault(x.Id)?.AbnormalFlag,
                results.GetValueOrDefault(x.Id)?.PerformedAt, x.Priority, x.ClinicalNote, x.StockItemId)).ToList(),
            priorEncounters,
            encounter.PresentingComplaintDurationDays,
            ParseClerkingDetails(encounter.ClerkingDetails));
    }

    public async Task<AdmissionDto> GetAdmissionAsync(Guid admissionId, CancellationToken cancellationToken = default)
    {
        var admission = await db.Admissions.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Encounters)
            .SingleOrDefaultAsync(x => x.Id == admissionId, cancellationToken)
            ?? throw new AppNotFoundException("Admission not found.");
        var doctorName = await db.Users.AsNoTracking().Where(x => x.Id == admission.AttendingDoctorId).Select(x => x.FullName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Unknown";
        var billId = await db.Bills.AsNoTracking()
            .Where(x => x.Kind == BillKind.Visit && x.AdmissionId == admissionId && x.Status != BillStatus.Void)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        return new AdmissionDto(
            admission.Id, admission.AdmissionNumber, admission.PatientId, admission.Patient?.FullName ?? "Unknown",
            admission.AttendingDoctorId, doctorName, admission.WardType,
            admission.BedNumber, admission.AdmittedAt, admission.DischargedAt, admission.Status,
            admission.DischargeSummary, billId, admission.Encounters.OrderBy(x => x.StartedAt).Select(x => x.Id).ToList());
    }

    /// <summary>
    /// Doctor worklist: checked-in appointments with no encounter yet, plus every non-discharged
    /// admission. This is a read-only view; opening an encounter happens through the normal
    /// encounter endpoints, which removes the patient from the queue on the next refresh.
    /// </summary>
    public async Task<DoctorQueueDto> GetDoctorQueueAsync(CancellationToken cancellationToken = default)
    {
        var outpatients = await db.Appointments.AsNoTracking()
            .Where(x => x.Status == AppointmentStatus.CheckedIn
                && !db.Encounters.Any(e => e.AppointmentId == x.Id && e.Status != EncounterStatus.Cancelled))
            .OrderBy(x => x.CheckedInAt ?? x.ScheduledAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.PatientId,
                x.Patient!.PatientNumber,
                PatientName = (x.Patient.FirstName + " " + (x.Patient.MiddleName ?? "") + " " + x.Patient.LastName).Trim(),
                x.AppointmentNumber,
                Since = x.CheckedInAt ?? x.ScheduledAt,
                HmoName = db.PatientHmoEnrollments
                    .Where(e => e.PatientId == x.PatientId && e.IsActive)
                    .OrderByDescending(e => e.IsPrimary)
                    .Select(e => e.HmoCompany!.Name).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var inpatients = await db.Admissions.AsNoTracking()
            .Where(x => x.Status == AdmissionStatus.Active || x.Status == AdmissionStatus.PendingDischarge)
            .OrderBy(x => x.AdmittedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.PatientId,
                x.Patient!.PatientNumber,
                PatientName = (x.Patient.FirstName + " " + (x.Patient.MiddleName ?? "") + " " + x.Patient.LastName).Trim(),
                x.AdmissionNumber,
                x.AdmittedAt,
                HmoName = db.PatientHmoEnrollments
                    .Where(e => e.PatientId == x.PatientId && e.IsActive)
                    .OrderByDescending(e => e.IsPrimary)
                    .Select(e => e.HmoCompany!.Name).FirstOrDefault(),
                OpenEncounterId = db.Encounters
                    .Where(e => e.AdmissionId == x.Id && e.Status == EncounterStatus.Open)
                    .Select(e => (Guid?)e.Id).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new DoctorQueueDto(
            outpatients.Select(x => new DoctorQueuePatientDto(x.Id, x.PatientId, x.PatientNumber, x.PatientName,
                x.AppointmentNumber, "Outpatient", x.Since, x.HmoName, false, null)).ToList(),
            inpatients.Select(x => new DoctorQueuePatientDto(x.Id, x.PatientId, x.PatientNumber, x.PatientName,
                x.AdmissionNumber, "Inpatient", x.AdmittedAt, x.HmoName,
                x.OpenEncounterId is not null, x.OpenEncounterId)).ToList());
    }

    /// <summary>
    /// Combined outpatient/inpatient worklist. Outpatient rows carry their appointment reference and
    /// inpatient rows carry their admission reference, so a single table can show both journeys.
    /// </summary>
    public async Task<IReadOnlyList<EncounterListItemDto>> SearchEncountersAsync(EncounterType? type,
        EncounterStatus? status, DateOnly? date, string? search, CancellationToken cancellationToken = default)
    {
        var query = db.Encounters.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Appointment)
            .Include(x => x.Admission)
            .AsQueryable();
        if (type is not null) query = query.Where(x => x.Type == type);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (date is not null) query = query.Where(x => x.StartedAt >= date.Value.ToDateTime(TimeOnly.MinValue)
            && x.StartedAt < date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.EncounterNumber, $"%{term}%")
                || EF.Functions.ILike(x.Patient!.PatientNumber, $"%{term}%")
                || EF.Functions.ILike(x.Patient.FirstName, $"%{term}%")
                || EF.Functions.ILike(x.Patient.LastName, $"%{term}%")
                || (x.Appointment != null && EF.Functions.ILike(x.Appointment.AppointmentNumber, $"%{term}%"))
                || (x.Admission != null && EF.Functions.ILike(x.Admission.AdmissionNumber, $"%{term}%")));
        }

        var rows = await query.OrderByDescending(x => x.StartedAt).Take(200).ToListAsync(cancellationToken);
        var doctorIds = rows.Select(x => x.DoctorId).Distinct().ToList();
        var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);

        return rows.Select(x => new EncounterListItemDto(
            x.Id, x.EncounterNumber, x.PatientId, x.Patient?.PatientNumber ?? string.Empty,
            x.Patient?.FullName ?? "Unknown", doctorNames.GetValueOrDefault(x.DoctorId, "Unknown"),
            x.Type, x.Status, x.IsEmergency, x.StartedAt,
            x.AppointmentId, x.Appointment?.AppointmentNumber, x.AdmissionId, x.Admission?.AdmissionNumber,
            x.PresentingComplaint)).ToList();
    }

    /// <summary>
    /// Persists the doctor's structured encounter clerking (subjective history, examination, assessment
    /// and plan). Any supplied medication, test or procedure row must be complete for both a draft save
    /// and signing; drafts stay editable while the encounter is open, and signing completes and locks it.
    /// </summary>
    public async Task<EncounterDto> SaveEncounterAsync(Guid encounterId, SaveEncounterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Complete && (string.IsNullOrWhiteSpace(request.PresentingComplaint) || string.IsNullOrWhiteSpace(request.Diagnosis) || request.Disposition is null))
            throw new AppConflictException("Signing requires a presenting complaint, diagnosis and disposition.");
        var encounter = await db.Encounters.SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        if (encounter.Status == EncounterStatus.Completed)
            throw new AppConflictException("A signed encounter is locked and cannot be edited.");
        if (encounter.Status == EncounterStatus.Cancelled)
            throw new AppConflictException("A cancelled encounter cannot be clerked.");

        encounter.PresentingComplaint = request.PresentingComplaint;
        encounter.PresentingComplaintDurationDays = request.PresentingComplaintDurationDays is >= 0
            ? request.PresentingComplaintDurationDays
            : null;
        encounter.HistoryOfPresentingComplaint = request.HistoryOfPresentingComplaint;
        encounter.PastMedicalSurgicalHistory = request.PastMedicalSurgicalHistory;
        encounter.FamilyHistory = request.FamilyHistory;
        encounter.Examination = request.Examination is { Count: > 0 }
            ? JsonSerializer.Serialize(request.Examination.Select(e => new ExaminationItemDto(e.Name, e.Result, e.Normal)))
            : null;
        // Keep ancillary clerking fields so the detail view can show everything captured by the form.
        if (request.Clerking is not null)
            encounter.ClerkingDetails = JsonSerializer.Serialize(request.Clerking);
        encounter.Diagnosis = request.Diagnosis;
        encounter.Disposition = request.Disposition is not null && Enum.TryParse<EncounterDisposition>(request.Disposition, out var disposition)
            ? disposition
            : null;
        encounter.UpdatedAt = DateTimeOffset.UtcNow;
        await ReconcilePlanOrdersAsync(encounter, request, cancellationToken);
        if (request.Complete)
        {
            encounter.Status = EncounterStatus.Completed;
            encounter.ClosedAt = DateTimeOffset.UtcNow;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (System.Exception e)
        {
            throw new AppConflictException("Encounter cannot be clerked - " + e.Message);
        }
        await audit.WriteAsync(request.Complete ? "encounter.clerked-signed" : "encounter.clerked-draft-saved",
            nameof(Encounter), encounter.Id.ToString(), encounter.PatientId, encounter.Id,
            after: new { encounter.Status }, cancellationToken: cancellationToken);
        if (request.Complete) await ApplyDispositionAsync(encounter, cancellationToken);
        return await GetEncounterAsync(encounter.Id, cancellationToken);
    }

    /// <summary>
    /// Applies the signed encounter's disposition: an outpatient marked Admit becomes an inpatient
    /// admission, and an inpatient marked Discharge closes treatment and starts billing (the visit
    /// bill is committed and the admission awaits Finance clearance).
    /// </summary>
    private async Task ApplyDispositionAsync(Encounter encounter, CancellationToken cancellationToken)
    {
        if (encounter.Disposition == EncounterDisposition.Admit && encounter.Type == EncounterType.Outpatient)
        {
            var activeAdmission = await db.Admissions.AsNoTracking()
                .Where(x => x.PatientId == encounter.PatientId
                    && (x.Status == AdmissionStatus.Active || x.Status == AdmissionStatus.PendingDischarge))
                .Select(x => new { x.Id, x.Status })
                .FirstOrDefaultAsync(cancellationToken);
            if (activeAdmission is null)
            {
                await CreateAdmissionAsync(new CreateAdmissionRequest(
                    encounter.PatientId, encounter.DoctorId, WardType.General, null,
                    encounter.HmoEnrollmentId, encounter.PresentingComplaint, null), cancellationToken);
            }
            else if (activeAdmission.Status == AdmissionStatus.Active)
            {
                await AddInpatientEncounterAsync(new CreateInpatientEncounterRequest(
                    activeAdmission.Id, encounter.DoctorId, encounter.PresentingComplaint), cancellationToken);
            }
            else
            {
                throw new AppConflictException("The patient's admission is awaiting discharge; complete it before admitting again.");
            }
        }
        else if (encounter.Disposition == EncounterDisposition.Discharge
            && encounter.Type == EncounterType.Inpatient && encounter.AdmissionId is not null)
        {
            var status = await db.Admissions.AsNoTracking()
                .Where(x => x.Id == encounter.AdmissionId)
                .Select(x => (AdmissionStatus?)x.Status)
                .FirstOrDefaultAsync(cancellationToken);
            if (status == AdmissionStatus.Active)
            {
                await RequestDischargeAsync(new RequestDischargeRequest(
                    encounter.AdmissionId.Value,
                    $"Discharged from {encounter.EncounterNumber}: {encounter.Diagnosis}"), cancellationToken);
            }
        }
    }

    private async Task ReconcilePlanOrdersAsync(Encounter encounter, SaveEncounterRequest request, CancellationToken cancellationToken)
    {
        var serviceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var fundingContext = await funding.VerifyAsync(encounter.FundingSource, encounter.HmoEnrollmentId,
            encounter.PatientId, serviceDate, cancellationToken);

        var medications = request.Medications;
        var tests = request.Tests;
        var procedures = request.Procedures;

        // Validate every referenced catalogue item before anything is written. A null collection means
        // "not supplied" (leave the encounter's orders alone); an empty collection means "clear".
        var stockIds = (medications ?? []).Select(x => x.StockItemId)
            .Concat((tests ?? []).Select(x => x.StockItemId))
            .Concat((procedures ?? []).Select(x => x.StockItemId))
            .Distinct().ToList();
        var catalogue = await db.StockItems.AsNoTracking()
            .Where(x => stockIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        ValidatePlanRows(medications, tests, procedures, catalogue);

        if (medications is not null)
            await ReconcilePrescriptionsAsync(encounter, medications, catalogue, cancellationToken);
        if (tests is not null)
            await ReconcileServiceRequestsAsync(encounter, tests, StockItemType.Test, fundingContext, serviceDate, catalogue, cancellationToken);
        if (procedures is not null)
            await ReconcileServiceRequestsAsync(encounter, procedures, StockItemType.Procedure, fundingContext, serviceDate, catalogue, cancellationToken);
    }

    private static void ValidatePlanRows(
        IReadOnlyList<PrescribeOrderRequest>? medications,
        IReadOnlyList<ServiceOrderRequest>? tests,
        IReadOnlyList<ServiceOrderRequest>? procedures,
        IReadOnlyDictionary<Guid, StockItem> catalogue)
    {
        for (var i = 0; i < (medications?.Count ?? 0); i++)
        {
            var item = catalogue.GetValueOrDefault(medications![i].StockItemId);
            if (item is null || item.ItemType != StockItemType.Medication || !item.IsActive)
                throw new AppConflictException($"Medication row {i + 1}: select an active medication from the stocks catalogue.");
        }

        ValidateServiceRows(tests, StockItemType.Test, "Test", catalogue);
        ValidateServiceRows(procedures, StockItemType.Procedure, "Procedure", catalogue);
    }

    private static void ValidateServiceRows(IReadOnlyList<ServiceOrderRequest>? rows, StockItemType kind,
        string label, IReadOnlyDictionary<Guid, StockItem> catalogue)
    {
        for (var i = 0; i < (rows?.Count ?? 0); i++)
        {
            var item = catalogue.GetValueOrDefault(rows![i].StockItemId);
            if (item is null || item.ItemType != kind || !item.IsActive)
                throw new AppConflictException($"{label} row {i + 1}: select an active {label.ToLowerInvariant()} from the stocks catalogue.");
        }
    }

    /// <summary>
    /// Keeps a single editable prescription per encounter. Rows are matched by item id (or stock item
    /// for older clients), updated in place, added, or removed so repeated draft saves never duplicate.
    /// </summary>
    private async Task ReconcilePrescriptionsAsync(Encounter encounter, IReadOnlyList<PrescribeOrderRequest> rows,
        IReadOnlyDictionary<Guid, StockItem> catalogue, CancellationToken cancellationToken)
    {
        var prescriptions = await db.Prescriptions
            .Include(x => x.Items)
            .Include(x => x.Dispenses)
            .Where(x => x.EncounterId == encounter.Id && x.Status != PrescriptionStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        // Only an un-dispensed prescription can be edited from the clerking form.
        var editable = prescriptions.FirstOrDefault(x =>
            x.Dispenses.Count == 0 && x.Items.All(item => item.QuantityDispensed == 0));

        if (rows.Count == 0)
        {
            if (editable is not null) db.Prescriptions.Remove(editable);
            return;
        }

        if (editable is null)
        {
            editable = new Prescription
            {
                EncounterId = encounter.Id,
                DoctorId = user.RequireId(),
                Status = PrescriptionStatus.SentToPharmacy,
                SentToPharmacyAt = DateTimeOffset.UtcNow,
                CreatedBy = user.RequireId()
            };
            db.Prescriptions.Add(editable);
        }

        var matched = new HashSet<Guid>();
        foreach (var row in rows)
        {
            var item = row.Id is { } itemId
                ? editable.Items.FirstOrDefault(x => x.Id == itemId)
                : editable.Items.FirstOrDefault(x => !matched.Contains(x.Id) && x.StockItemId == row.StockItemId);
            if (row.Id is not null && item is null)
                throw new AppConflictException("A medication row no longer belongs to this encounter. Reload and try again.");

            if (item is null)
            {
                item = new PrescriptionItem { PrescriptionId = editable.Id, StockItemId = row.StockItemId };
                editable.Items.Add(item);
            }
            else if (item.QuantityDispensed > 0)
            {
                throw new AppConflictException(
                    $"'{catalogue.GetValueOrDefault(item.StockItemId)?.Name ?? "This medication"}' has already been dispensed and cannot be edited.");
            }

            matched.Add(item.Id);
            item.StockItemId = row.StockItemId;
            item.Dose = row.Dose.Trim();
            item.Route = row.Route;
            item.Frequency = row.Frequency;
            item.DurationDays = row.DurationDays;
            item.QuantityPrescribed = row.Quantity;
            item.Instructions = string.IsNullOrWhiteSpace(row.Instructions) ? null : row.Instructions.Trim();
        }

        foreach (var stale in editable.Items.Where(x => !matched.Contains(x.Id)).ToList())
        {
            if (stale.QuantityDispensed > 0)
                throw new AppConflictException("A dispensed medication cannot be removed from the encounter.");
            editable.Items.Remove(stale);
            db.PrescriptionItems.Remove(stale);
        }
    }

    /// <summary>
    /// Reconciles a doctor's test or procedure orders. Removals are blocked once a result has been
    /// recorded or the order has been charged so bill lines can never be orphaned.
    /// </summary>
    private async Task ReconcileServiceRequestsAsync(Encounter encounter, IReadOnlyList<ServiceOrderRequest> rows,
        StockItemType kind, FundingContext fundingContext, DateOnly serviceDate,
        IReadOnlyDictionary<Guid, StockItem> catalogue, CancellationToken cancellationToken)
    {
        var existing = await db.ServiceRequests
            .Include(x => x.StockItem)
            .Where(x => x.EncounterId == encounter.Id && x.Kind == kind && x.Status != ServiceRequestStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var existingIds = existing.Select(x => x.Id).ToList();
        var results = await db.ServiceResults
            .Where(x => existingIds.Contains(x.RequestId))
            .ToDictionaryAsync(x => x.RequestId, cancellationToken);

        var matched = new HashSet<Guid>();
        foreach (var row in rows)
        {
            ServiceRequest? request;
            if (row.Id is { } requestId)
            {
                request = existing.FirstOrDefault(x => x.Id == requestId)
                    ?? throw new AppConflictException($"A {kind} row no longer belongs to this encounter. Reload and try again.");
            }
            else
            {
                request = existing.FirstOrDefault(x => !matched.Contains(x.Id) && x.StockItemId == row.StockItemId);
            }

            if (request is null)
            {
                var price = await funding.ResolvePriceAsync(row.StockItemId, fundingContext, serviceDate, cancellationToken)
                    ?? throw new AppConflictException($"No {fundingContext.Source} price is configured for {catalogue[row.StockItemId].Name}.");
                request = new ServiceRequest
                {
                    RequestNumber = await numbers.NextAsync("service_request_number_seq", "SR", cancellationToken),
                    EncounterId = encounter.Id,
                    PatientId = encounter.PatientId,
                    StockItemId = row.StockItemId,
                    Kind = kind,
                    DoctorId = user.RequireId(),
                    FundingSource = fundingContext.Source,
                    HmoEnrollmentId = fundingContext.HmoEnrollmentId,
                    HmoCompanyId = fundingContext.HmoCompanyId,
                    ItemPriceId = price.Id,
                    UnitPriceSnapshot = price.UnitPrice,
                    Priority = NormalizePriority(row.Priority),
                    ClinicalNote = NormalizeNote(row.ClinicalNote),
                    CreatedBy = user.RequireId()
                };
                db.ServiceRequests.Add(request);
                db.ServiceResults.Add(new ServiceResult { RequestId = request.Id, Status = ServiceResultStatus.Pending });
                matched.Add(request.Id);
                continue;
            }

            var currentName = request.StockItem?.Name ?? catalogue.GetValueOrDefault(request.StockItemId)?.Name ?? kind.ToString();
            var result = results.GetValueOrDefault(request.Id);
            if (result?.Status is ServiceResultStatus.Submitted or ServiceResultStatus.Amended)
                throw new AppConflictException($"'{currentName}' has a submitted result and cannot be changed.");
            if (request.BillLineCreatedAt is not null)
                throw new AppConflictException($"'{currentName}' has already been charged and cannot be changed.");

            if (request.StockItemId != row.StockItemId)
            {
                var price = await funding.ResolvePriceAsync(row.StockItemId, fundingContext, serviceDate, cancellationToken)
                    ?? throw new AppConflictException($"No {fundingContext.Source} price is configured for {catalogue[row.StockItemId].Name}.");
                request.StockItemId = row.StockItemId;
                request.ItemPriceId = price.Id;
                request.UnitPriceSnapshot = price.UnitPrice;
            }
            request.Priority = NormalizePriority(row.Priority);
            request.ClinicalNote = NormalizeNote(row.ClinicalNote);
            request.UpdatedAt = DateTimeOffset.UtcNow;
            matched.Add(request.Id);
        }

        foreach (var stale in existing.Where(x => !matched.Contains(x.Id)))
        {
            var name = stale.StockItem?.Name ?? catalogue.GetValueOrDefault(stale.StockItemId)?.Name ?? kind.ToString();
            var result = results.GetValueOrDefault(stale.Id);
            if (result?.Status is ServiceResultStatus.Entered or ServiceResultStatus.Submitted or ServiceResultStatus.Amended)
                throw new AppConflictException($"'{name}' already has a recorded result and cannot be removed.");
            if (stale.BillLineCreatedAt is not null)
                throw new AppConflictException($"'{name}' has already been charged and cannot be removed.");
            if (result is not null) db.ServiceResults.Remove(result);
            db.ServiceRequests.Remove(stale);
        }
    }

    private static string? NormalizePriority(string? priority) =>
        string.IsNullOrWhiteSpace(priority) ? "Routine" : priority.Trim();

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    private static IReadOnlyList<ExaminationItemDto> ParseExamination(string? json) =>
        string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<ExaminationItemDto>>(json) ?? [];

    private static readonly JsonSerializerOptions _caseInsensitive = new() { PropertyNameCaseInsensitive = true };

    private static ClerkingDetailsDto? ParseClerkingDetails(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<ClerkingDetailsDto>(json, _caseInsensitive);

    private static string? SanitizeText(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > maxLength ? value.Trim().Substring(0, maxLength) : value.Trim();
}
