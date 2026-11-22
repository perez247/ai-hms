using System.Text.RegularExpressions;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>
/// Nurse workflows. Vitals can be attached directly to a patient without requiring an encounter;
/// encounter services expose prescription items and the latest pharmacy dispense for administration.
/// </summary>
public sealed class NurseService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    IPatientService patients) : INurseService
{
    public async Task<PatientVitalsPageDto> GetVitalsAsync(string patientNumber, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Vitals.AsNoTracking().Where(x => x.PatientId == patient.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.RecordedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PatientVitalsPageDto(page, pageSize, total, rows.Select(MapVital).ToList());
    }

    public async Task<VitalDto> RecordPatientVitalsAsync(RecordPatientVitalsRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(request.PatientNumber, cancellationToken);
        if (request.TemperatureC is < 25m or > 45m) throw new AppConflictException("Temperature is outside a plausible range.");
        if (request.PainScore is < 0 or > 10) throw new AppConflictException("Pain score must be between 0 and 10.");
        var vital = new Vital
        {
            PatientId = patient.Id,
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
        await audit.WriteAsync("vitals.recorded", nameof(Vital), vital.Id.ToString(), patient.Id, cancellationToken: cancellationToken);
        return MapVital(vital);
    }

    public async Task<IReadOnlyList<NurseAppointmentDto>> GetScheduledAppointmentsAsync(CancellationToken cancellationToken = default)
    {
        var appointments = await db.Appointments.AsNoTracking().Include(x => x.Patient)
            .Where(x => x.Status != AppointmentStatus.Completed && x.Status != AppointmentStatus.Cancelled && x.Status != AppointmentStatus.NoShow)
            .OrderBy(x => x.ScheduledAt).Take(100).ToListAsync(cancellationToken);
        var doctorIds = appointments.Select(x => x.DoctorId).Distinct().ToList();
        var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        return appointments.Select(x => new NurseAppointmentDto(
            x.Id, x.AppointmentNumber, x.PatientId, x.Patient?.PatientNumber ?? "—", x.Patient?.FullName ?? "Unknown",
            x.DoctorId, doctorNames.GetValueOrDefault(x.DoctorId, "Unknown"), x.ScheduledAt, x.Status.ToString(), x.Reason)).ToList();
    }

    public async Task<IReadOnlyList<NurseInpatientDto>> GetInpatientsAsync(CancellationToken cancellationToken = default)
    {
        var admissions = await db.Admissions.AsNoTracking().Include(x => x.Patient).Include(x => x.Encounters)
            .Where(x => x.Status == AdmissionStatus.Active || x.Status == AdmissionStatus.PendingDischarge)
            .OrderBy(x => x.AdmittedAt).Take(100).ToListAsync(cancellationToken);
        var doctorIds = admissions.Select(x => x.AttendingDoctorId).Distinct().ToList();
        var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        return admissions.Select(x => new NurseInpatientDto(
            x.Id, x.AdmissionNumber, x.PatientId, x.Patient?.PatientNumber ?? "—", x.Patient?.FullName ?? "Unknown",
            doctorNames.GetValueOrDefault(x.AttendingDoctorId, "Unknown"), x.WardType.ToString(), x.BedNumber,
            x.AdmittedAt, x.Status.ToString(), x.Encounters.Select(e => e.Id).ToList())).ToList();
    }

    public async Task<IReadOnlyList<NurseEncounterDto>> GetOpenEncountersAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        var encounters = await db.Encounters.AsNoTracking()
            .Include(x => x.Admission)
            .Include(x => x.Prescriptions).ThenInclude(x => x.Items).ThenInclude(x => x.Medication)
            .Where(x => x.PatientId == patient.Id && x.Status == EncounterStatus.Open
                && (x.Admission == null || x.Admission.Status == AdmissionStatus.Active || x.Admission.Status == AdmissionStatus.PendingDischarge))
            .OrderByDescending(x => x.StartedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var doctorIds = encounters.Select(x => x.DoctorId).Distinct().ToList();
        var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        var encounterIds = encounters.Select(x => x.Id).ToList();
        var dispenseRows = await db.DispenseItems.AsNoTracking()
            .Include(x => x.Dispense)
            .Where(x => encounterIds.Contains(x.Dispense!.EncounterId))
            .OrderByDescending(x => x.Dispense!.CreatedAt)
            .ToListAsync(cancellationToken);
        var latestDispenseByItem = dispenseRows.GroupBy(x => x.PrescriptionItemId).ToDictionary(g => g.Key, g => g.First().Id);

        return encounters.Select(encounter => new NurseEncounterDto(
            encounter.Id, encounter.EncounterNumber, encounter.Type.ToString(), encounter.Status.ToString(),
            doctorNames.GetValueOrDefault(encounter.DoctorId, "Unknown"), encounter.StartedAt,
            encounter.Admission?.Status.ToString(),
            encounter.Prescriptions.SelectMany(p => p.Items).Select(item => new NursePrescriptionItemDto(
                item.Id, latestDispenseByItem.GetValueOrDefault(item.Id), item.Medication?.Name ?? "Medication",
                item.Dose, item.Route, item.Frequency, item.QuantityPrescribed, item.QuantityDispensed,
                latestDispenseByItem.ContainsKey(item.Id))).ToList())).ToList();
    }

    public async Task<IReadOnlyList<ShiftHandoverDto>> GetHandoversAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await db.ShiftHandovers.AsNoTracking()
            .Join(db.Users.AsNoTracking(), h => h.NurseId, u => u.Id, (h, u) => new { h, u })
            .OrderByDescending(x => x.h.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ShiftHandoverDto(x.h.Id, x.h.NurseId, x.u.FullName, x.h.ShiftDate, x.h.ContentHtml, x.h.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ShiftHandoverDto> CreateHandoverAsync(CreateShiftHandoverRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ContentHtml)) throw new AppConflictException("Handover content is required.");
        var content = SanitizeHtml(request.ContentHtml);
        if (StripTags(content).Length == 0) throw new AppConflictException("Handover content is required.");
        var handover = new ShiftHandover
        {
            NurseId = user.RequireId(),
            ShiftDate = request.ShiftDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            ContentHtml = content
        };
        db.ShiftHandovers.Add(handover);
        await db.SaveChangesAsync(cancellationToken);
        var nurseName = await db.Users.AsNoTracking().Where(x => x.Id == handover.NurseId).Select(x => x.FullName).SingleAsync(cancellationToken);
        await audit.WriteAsync("nursing.shift-handover", nameof(ShiftHandover), handover.Id.ToString(), cancellationToken: cancellationToken);
        return new ShiftHandoverDto(handover.Id, handover.NurseId, nurseName, handover.ShiftDate, handover.ContentHtml, handover.CreatedAt);
    }

    private static string SanitizeHtml(string html)
    {
        var clean = Regex.Replace(html, "<script.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        clean = Regex.Replace(clean, "<style.*?</style>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        clean = Regex.Replace(clean, "\\son\\w+\\s*=\\s*(['\"]).*?\\1", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return clean.Trim();
    }

    private static string StripTags(string html) => Regex.Replace(html, "<.*?>", string.Empty).Trim();

    private static VitalDto MapVital(Vital x) => new(
        x.Id, x.RecordedAt, x.TemperatureC, x.Systolic, x.Diastolic, x.Pulse, x.RespiratoryRate,
        x.Spo2, x.WeightKg, x.HeightCm, x.PainScore, x.Notes);
}
