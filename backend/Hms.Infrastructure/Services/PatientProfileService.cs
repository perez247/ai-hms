using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>
/// Builds the permission-filtered patient profile. The caller has already passed the
/// patients.profile.read policy; this service only decides which sections may be serialized.
/// </summary>
public sealed class PatientProfileService(
    HmsDbContext db,
    ICurrentUser user,
    IPatientService patients,
    IFitnessService fitness,
    IBillingService billing,
    IFileStorage files,
    IAuditWriter audit) : IPatientProfileService
{
    public async Task<PatientProfileDto> GetAsync(Guid patientId, int vitalsPage, int vitalsPageSize, CancellationToken cancellationToken = default)
    {
        var role = user.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        var patient = await patients.GetAsync(patientId, cancellationToken);
        var activeHmo = await db.PatientHmoEnrollments.AsNoTracking()
            .AnyAsync(x => x.PatientId == patientId && x.IsActive, cancellationToken);
        var isPrivate = !activeHmo;

        PatientVitalsPageDto? vitals = null;
        if (ProfileAccess.CanViewVitals(role))
            vitals = await GetVitalsAsync(patientId, vitalsPage, vitalsPageSize, cancellationToken);

        IReadOnlyList<FitnessCertificateDto>? fitnessRecords = null;
        if (ProfileAccess.CanViewFitness(role))
            fitnessRecords = await fitness.GetForPatientAsync(patientId, cancellationToken);

        IReadOnlyList<PatientEncounterSummaryDto>? encounters = null;
        IReadOnlyList<PatientInvestigationDto>? investigations = null;
        IReadOnlyList<PatientSurgeryDto>? surgeries = null;
        if (ProfileAccess.CanViewClinical(role))
        {
            var rows = await db.Encounters.AsNoTracking().Where(x => x.PatientId == patientId)
                .OrderByDescending(x => x.StartedAt).ToListAsync(cancellationToken);
            var doctorIds = rows.Select(x => x.DoctorId).Distinct().ToList();
            var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
                .Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
            encounters = rows.Select(x => new PatientEncounterSummaryDto(
                x.Id, x.EncounterNumber, x.Type.ToString(), x.Status.ToString(),
                doctorNames.GetValueOrDefault(x.DoctorId, "Unknown"), x.StartedAt, x.ChiefComplaint)).ToList();

            var investigationRows = await db.InvestigationOrders.AsNoTracking()
                .Include(x => x.CatalogItem).Include(x => x.Encounter)
                .Where(x => x.Encounter!.PatientId == patientId)
                .OrderByDescending(x => x.OrderedAt).ToListAsync(cancellationToken);
            investigations = investigationRows.Select(x => new PatientInvestigationDto(
                x.Id, x.CatalogItem?.Name ?? "Investigation", x.Status.ToString(), x.Encounter?.EncounterNumber ?? "—",
                x.OrderedAt, x.PerformedAt, x.ReportText, x.ReferralDestination)).ToList();

            var surgeryRows = await db.SurgeryCases.AsNoTracking()
                .Include(x => x.CatalogItem).Include(x => x.Encounter)
                .Where(x => x.Encounter!.PatientId == patientId)
                .OrderByDescending(x => x.ScheduledAt).ToListAsync(cancellationToken);
            surgeries = surgeryRows.Select(x => new PatientSurgeryDto(
                x.Id, x.CatalogItem?.Name ?? "Surgery", x.Status.ToString(), x.Encounter?.EncounterNumber ?? "—",
                x.ScheduledAt, x.PerformedAt, x.ReportText)).ToList();
        }

        PatientFinanceDto? finance = null;
        if (ProfileAccess.CanViewFinance(role, isPrivate))
        {
            var bills = await billing.SearchAsync(null, patientId, 1, 100, cancellationToken);
            var canSeeBills = ProfileAccess.CanViewFinance(role, false);
            finance = new PatientFinanceDto(
                bills.Items.Sum(x => x.PatientPayableTotal + x.HmoPayableTotal),
                bills.Items.Sum(x => x.PaidTotal),
                bills.Items.Sum(x => x.Balance),
                isPrivate ? "Private" : "HMO",
                canSeeBills ? bills.Items : null);
        }

        var patientFiles = await db.PatientFiles.AsNoTracking().Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var fileDtos = patientFiles.Select(x => new PatientFileDto(
            x.Id, x.FileName, x.ContentType, x.SizeBytes, x.CreatedAt, x.UploadedBy)).ToList();

        return new PatientProfileDto(patient, isPrivate, vitals, fitnessRecords, encounters, investigations, surgeries,
            finance, fileDtos, ProfileAccess.CanUploadPatientFiles(role));
    }

    public async Task<PatientBasicProfileDto> GetBasicByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var role = user.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        var hasHmo = await db.PatientHmoEnrollments.AsNoTracking().AnyAsync(x => x.PatientId == patient.Id, cancellationToken);
        // HMO assignments are an independent lazy accordion; do not ship them with basic demographics.
        return new PatientBasicProfileDto(patient with { HmoEnrollments = [] }, !hasHmo, ProfileAccess.CanUploadPatientFiles(role));
    }

    public async Task<IReadOnlyList<PatientHmoEnrollmentDto>> GetHmosByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        return patient.HmoEnrollments.ToList();
    }

    public async Task<PatientVitalsPageDto> GetVitalsByNumberAsync(string patientNumber, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        return await GetVitalsAsync(patient.Id, page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<FitnessCertificateDto>> GetFitnessByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        return await fitness.GetForPatientAsync(patient.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientEncounterSummaryDto>> GetEncountersByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        RequireClinicalAccess();
        var rows = await db.Encounters.AsNoTracking().Where(x => x.PatientId == patient.Id)
            .OrderByDescending(x => x.StartedAt).ToListAsync(cancellationToken);
        var doctorIds = rows.Select(x => x.DoctorId).Distinct().ToList();
        var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        return rows.Select(x => new PatientEncounterSummaryDto(
            x.Id, x.EncounterNumber, x.Type.ToString(), x.Status.ToString(),
            doctorNames.GetValueOrDefault(x.DoctorId, "Unknown"), x.StartedAt, x.ChiefComplaint)).ToList();
    }

    public async Task<IReadOnlyList<PatientInvestigationDto>> GetInvestigationsByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        RequireClinicalAccess();
        var rows = await db.InvestigationOrders.AsNoTracking()
            .Include(x => x.CatalogItem).Include(x => x.Encounter)
            .Where(x => x.Encounter!.PatientId == patient.Id)
            .OrderByDescending(x => x.OrderedAt).ToListAsync(cancellationToken);
        return rows.Select(x => new PatientInvestigationDto(
            x.Id, x.CatalogItem?.Name ?? "Investigation", x.Status.ToString(), x.Encounter?.EncounterNumber ?? "—",
            x.OrderedAt, x.PerformedAt, x.ReportText, x.ReferralDestination)).ToList();
    }

    public async Task<IReadOnlyList<PatientSurgeryDto>> GetSurgeriesByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        RequireClinicalAccess();
        var rows = await db.SurgeryCases.AsNoTracking()
            .Include(x => x.CatalogItem).Include(x => x.Encounter)
            .Where(x => x.Encounter!.PatientId == patient.Id)
            .OrderByDescending(x => x.ScheduledAt).ToListAsync(cancellationToken);
        return rows.Select(x => new PatientSurgeryDto(
            x.Id, x.CatalogItem?.Name ?? "Surgery", x.Status.ToString(), x.Encounter?.EncounterNumber ?? "—",
            x.ScheduledAt, x.PerformedAt, x.ReportText)).ToList();
    }

    public async Task<PatientFinanceDto?> GetFinanceByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var role = user.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        var isPrivate = !await db.PatientHmoEnrollments.AsNoTracking().AnyAsync(x => x.PatientId == patient.Id, cancellationToken);
        if (!ProfileAccess.CanViewFinance(role, isPrivate)) return null;
        var bills = await billing.SearchAsync(null, patient.Id, 1, 100, cancellationToken);
        var canSeeBills = ProfileAccess.CanViewFinance(role, false);
        return new PatientFinanceDto(
            bills.Items.Sum(x => x.PatientPayableTotal + x.HmoPayableTotal),
            bills.Items.Sum(x => x.PaidTotal), bills.Items.Sum(x => x.Balance),
            isPrivate ? "Private" : "HMO", canSeeBills ? bills.Items : null);
    }

    public async Task<IReadOnlyList<PatientFileDto>> GetFilesByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        return await db.PatientFiles.AsNoTracking().Where(x => x.PatientId == patient.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PatientFileDto(x.Id, x.FileName, x.ContentType, x.SizeBytes, x.CreatedAt, x.UploadedBy))
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientFileDto> UploadFileByNumberAsync(string patientNumber, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        return await UploadFileAsync(patient.Id, fileName, contentType, content, cancellationToken);
    }

    public async Task<PatientFileContentDto> DownloadFileByNumberAsync(string patientNumber, Guid fileId, CancellationToken cancellationToken = default)
    {
        var patient = await patients.GetByNumberAsync(patientNumber, cancellationToken);
        return await DownloadFileAsync(patient.Id, fileId, cancellationToken);
    }

    public async Task<PatientVitalsPageDto> GetVitalsAsync(Guid patientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var role = user.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        if (!ProfileAccess.CanViewVitals(role)) throw new AppForbiddenException("You are not allowed to view patient vitals.");
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Vitals.AsNoTracking().Where(x => x.Encounter!.PatientId == patientId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.RecordedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PatientVitalsPageDto(page, pageSize, total, rows.Select(MapVital).ToList());
    }

    public async Task<PatientFileDto> UploadFileAsync(Guid patientId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        var role = user.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        if (!ProfileAccess.CanUploadPatientFiles(role)) throw new AppForbiddenException("You are not allowed to upload patient files.");
        if (!await db.Patients.AnyAsync(x => x.Id == patientId, cancellationToken)) throw new AppNotFoundException("Patient not found.");
        if (string.IsNullOrWhiteSpace(fileName)) throw new AppConflictException("File name is required.");
        var stored = await files.SaveAsync("patients", patientId, fileName, content, cancellationToken);
        var file = new PatientFile
        {
            PatientId = patientId,
            FileName = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = stored.SizeBytes,
            StorageKey = stored.StorageKey,
            UploadedBy = user.RequireId()
        };
        db.PatientFiles.Add(file);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("patient.file-uploaded", nameof(PatientFile), file.Id.ToString(), patientId, cancellationToken: cancellationToken);
        return new PatientFileDto(file.Id, file.FileName, file.ContentType, file.SizeBytes, file.CreatedAt, file.UploadedBy);
    }

    public async Task<PatientFileContentDto> DownloadFileAsync(Guid patientId, Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await db.PatientFiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == fileId && x.PatientId == patientId, cancellationToken)
            ?? throw new AppNotFoundException("Patient file not found.");
        var stream = await files.OpenReadAsync(file.StorageKey, cancellationToken);
        return new PatientFileContentDto(file.FileName, file.ContentType, stream);
    }

    private void RequireClinicalAccess()
    {
        var role = user.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        if (!ProfileAccess.CanViewClinical(role)) throw new AppForbiddenException("You are not allowed to view clinical sections.");
    }

    private static VitalDto MapVital(Vital x) => new(
        x.Id, x.RecordedAt, x.TemperatureC, x.Systolic, x.Diastolic, x.Pulse, x.RespiratoryRate,
        x.Spo2, x.WeightKg, x.HeightCm, x.PainScore, x.Notes);
}
