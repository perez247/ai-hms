using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Outpatient/inpatient encounter creation and reads.</summary>
public sealed partial class ClinicalService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    INumberGenerator numbers,
    IBillingService billing) : IClinicalService
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
        var encounter = new Encounter
        {
            EncounterNumber = await numbers.NextAsync("encounter_number_seq", "ENC", cancellationToken),
            PatientId = patient.Id,
            DoctorId = request.DoctorId,
            AppointmentId = appointment?.Id,
            Type = EncounterType.Outpatient,
            IsEmergency = request.IsEmergency,
            HmoEnrollmentId = request.HmoEnrollmentId,
            ChiefComplaint = request.ChiefComplaint,
            CreatedBy = user.RequireId()
        };
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);
        await billing.GetOrCreateEncounterBillAsync(encounter, cancellationToken);

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
            HmoEnrollmentId = request.HmoEnrollmentId,
            ChiefComplaint = request.ChiefComplaint,
            CreatedBy = user.RequireId()
        };
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);
        var bill = await billing.GetOrCreateAdmissionBillAsync(admission, cancellationToken);
        await AddConsultationChargeAsync(bill.Id, encounter, cancellationToken);
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
            ChiefComplaint = request.ChiefComplaint,
            CreatedBy = user.RequireId()
        };
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);
        var bill = await billing.GetOrCreateAdmissionBillAsync(admission, cancellationToken);
        await AddConsultationChargeAsync(bill.Id, encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("encounter.inpatient-created", nameof(Encounter), encounter.Id.ToString(), admission.PatientId, encounter.Id, cancellationToken: cancellationToken);
        return await GetEncounterAsync(encounter.Id, cancellationToken);
    }

    public async Task<EncounterDto> GetEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Vitals)
            .Include(x => x.Prescriptions)
            .Include(x => x.Investigations).ThenInclude(x => x.CatalogItem)
            .Include(x => x.Surgeries).ThenInclude(x => x.CatalogItem)
            .SingleOrDefaultAsync(x => x.Id == encounterId, cancellationToken)
            ?? throw new AppNotFoundException("Encounter not found.");
        var doctorName = await db.Users.AsNoTracking().Where(x => x.Id == encounter.DoctorId).Select(x => x.FullName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Unknown";
        var billId = await db.Bills.AsNoTracking()
            .Where(x => x.Status != BillStatus.Void && ((x.Kind == BillKind.OutpatientEncounter && x.EncounterId == encounterId)
                || (x.Kind == BillKind.InpatientAdmission && x.AdmissionId == encounter.AdmissionId)))
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        return new EncounterDto(
            encounter.Id, encounter.EncounterNumber, encounter.PatientId, encounter.Patient?.FullName ?? "Unknown",
            encounter.DoctorId, doctorName, encounter.Type, encounter.Status,
            encounter.IsEmergency, encounter.StartedAt, encounter.AdmissionId, billId,
            encounter.Vitals.OrderByDescending(x => x.RecordedAt).Select(MapVital).ToList(),
            encounter.Prescriptions.Select(x => new PrescriptionSummaryDto(x.Id, x.Status, x.CreatedAt, x.Items.Count)).ToList(),
            encounter.Investigations.Select(x => new InvestigationSummaryDto(x.Id, x.CatalogItem?.Name ?? "Investigation", x.Status, x.OrderedAt)).ToList(),
            encounter.Surgeries.Select(x => new SurgerySummaryDto(x.Id, x.CatalogItem?.Name ?? "Surgery", x.Status, x.ScheduledAt)).ToList());
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
            .Where(x => x.Kind == BillKind.InpatientAdmission && x.AdmissionId == admissionId && x.Status != BillStatus.Void)
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
}
