using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Registration/renewal, HMO enrollment and patient search/read projections.</summary>
public sealed class PatientService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    INumberGenerator numbers,
    IBillingService billing) : IPatientService
{
    public async Task<PagedResult<PatientSearchItem>> SearchAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.FirstName, term)
                || EF.Functions.ILike(x.LastName, term)
                || EF.Functions.ILike(x.PatientNumber, term)
                || (x.Phone != null && EF.Functions.ILike(x.Phone, term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PatientSearchItem(x.Id, x.PatientNumber, (x.FirstName + " " + (x.MiddleName ?? "") + " " + x.LastName).Trim(), x.Phone, x.CurrentRegistrationExpiresAt, x.Status))
            .ToListAsync(cancellationToken);
        return new PagedResult<PatientSearchItem>(items, page, pageSize, total);
    }

    public async Task<PatientDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking()
            .Include(x => x.HmoEnrollments).ThenInclude(x => x.HmoCompany)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        return Map(patient);
    }

    /// <summary>Receptionist-editable basic demographics; clinical records are untouched.</summary>
    public async Task<PatientDto> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.Include(x => x.HmoEnrollments).ThenInclude(x => x.HmoCompany)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new AppConflictException("First name and last name are required.");
        if (request.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new AppConflictException("Date of birth must be in the past.");

        patient.FirstName = request.FirstName.Trim();
        patient.MiddleName = request.MiddleName?.Trim();
        patient.LastName = request.LastName.Trim();
        patient.Sex = request.Sex;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Phone = request.Phone?.Trim();
        patient.Email = request.Email?.Trim();
        patient.Address = request.Address?.Trim();
        patient.StateOfOrigin = request.StateOfOrigin?.Trim();
        patient.HomeTown = request.HomeTown?.Trim();
        patient.Allergy = request.Allergy?.Trim();
        patient.Occupation = request.Occupation?.Trim();
        patient.NextOfKinName = request.NextOfKinName?.Trim();
        patient.NextOfKinPhone = request.NextOfKinPhone?.Trim();
        patient.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("patient.updated", nameof(Patient), patient.Id.ToString(), patient.Id,
            after: new { patient.FirstName, patient.LastName, patient.StateOfOrigin, patient.HomeTown, patient.Allergy, patient.Occupation },
            cancellationToken: cancellationToken);
        return await GetAsync(patientId, cancellationToken);
    }

    /// <summary>Registration, current period and registration bill are committed atomically.</summary>
    public async Task<PatientDto> GetByNumberAsync(string patientNumber, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking()
            .Include(x => x.HmoEnrollments).ThenInclude(x => x.HmoCompany)
            .SingleOrDefaultAsync(x => x.PatientNumber == patientNumber, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        return Map(patient);
    }

    public async Task<Guid> ResolveIdByNumberAsync(string patientNumber, CancellationToken cancellationToken = default) =>
        await db.Patients.Where(x => x.PatientNumber == patientNumber).Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken) is var id && id != Guid.Empty
            ? id
            : throw new AppNotFoundException("Patient not found.");

    public async Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new AppConflictException("First name and last name are required.");
        if (request.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new AppConflictException("Date of birth must be in the past.");

        var enrollments = request.HmoEnrollments ?? [];
        if (enrollments.Count(x => x.IsPrimary) > 1)
            throw new AppConflictException("Only one HMO enrollment may be primary.");
        await ValidateEnrollmentsAsync(enrollments, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var months = await GetRegistrationMonthsAsync(cancellationToken);
        var patient = new Patient
        {
            PatientNumber = await numbers.NextAsync("patient_number_seq", "PAT", cancellationToken),
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            LastName = request.LastName.Trim(),
            Sex = request.Sex,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            StateOfOrigin = request.StateOfOrigin?.Trim(),
            HomeTown = request.HomeTown?.Trim(),
            Allergy = request.Allergy?.Trim(),
            Occupation = request.Occupation?.Trim(),
            NextOfKinName = request.NextOfKinName,
            NextOfKinPhone = request.NextOfKinPhone,
            RegistrationDate = today,
            CurrentRegistrationExpiresAt = today.AddMonths(months),
            Status = PatientStatus.Active,
            CreatedBy = user.RequireId()
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);

        var period = new PatientRegistrationPeriod
        {
            PatientId = patient.Id,
            PeriodStart = today,
            PeriodEnd = patient.CurrentRegistrationExpiresAt,
            IsCurrent = true,
            ProcessedBy = user.RequireId()
        };
        db.PatientRegistrationPeriods.Add(period);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var enrollment in enrollments)
        {
            db.PatientHmoEnrollments.Add(new PatientHmoEnrollment
            {
                PatientId = patient.Id,
                HmoCompanyId = enrollment.HmoCompanyId,
                HmoMemberId = enrollment.HmoMemberId.Trim(),
                PlanName = enrollment.PlanName,
                ValidFrom = enrollment.ValidFrom,
                ValidTo = enrollment.ValidTo,
                IsPrimary = enrollment.IsPrimary,
                CreatedBy = user.RequireId()
            });
        }

        var bill = await billing.CreateAdministrativeBillAsync(
            patient.Id, BillKind.Registration, FeeCode.RegistrationNew, period.Id,
            ChargeSourceType.RegistrationPeriod, "Patient registration", cancellationToken);
        period.FeeAmount = bill.PatientPayableTotal;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await audit.WriteAsync("patient.created", nameof(Patient), patient.Id.ToString(), patient.Id,
            after: new { patient.PatientNumber, patient.FullName, patient.CurrentRegistrationExpiresAt }, cancellationToken: cancellationToken);

        var saved = await db.Patients.Include(x => x.HmoEnrollments).ThenInclude(x => x.HmoCompany)
            .SingleAsync(x => x.Id == patient.Id, cancellationToken);
        return Map(saved);
    }

    /// <summary>Renewal starts at the later of today or current expiry and creates a new fee bill.</summary>
    public async Task<RegistrationRenewalResult> RenewRegistrationAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.Include(x => x.RegistrationPeriods)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var months = await GetRegistrationMonthsAsync(cancellationToken);
        var current = patient.RegistrationPeriods.SingleOrDefault(x => x.IsCurrent);
        var start = current is not null && current.PeriodEnd > today ? current.PeriodEnd : today;
        var end = start.AddMonths(months);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (current is not null) current.IsCurrent = false;
        var period = new PatientRegistrationPeriod
        {
            PatientId = patient.Id,
            PeriodStart = start,
            PeriodEnd = end,
            IsCurrent = true,
            RenewalOfId = current?.Id,
            ProcessedBy = user.RequireId()
        };
        db.PatientRegistrationPeriods.Add(period);
        await db.SaveChangesAsync(cancellationToken);

        var bill = await billing.CreateAdministrativeBillAsync(
            patient.Id, BillKind.Registration, FeeCode.RegistrationRenewal, period.Id,
            ChargeSourceType.RegistrationPeriod, "Patient registration renewal", cancellationToken);
        period.FeeAmount = bill.PatientPayableTotal;
        patient.CurrentRegistrationExpiresAt = end;
        patient.Status = PatientStatus.Active;
        patient.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await audit.WriteAsync("patient.registration-renewed", nameof(Patient), patient.Id.ToString(), patient.Id,
            after: new { period.PeriodStart, period.PeriodEnd, bill.BillNumber }, cancellationToken: cancellationToken);
        return new RegistrationRenewalResult(patient.Id, end, bill.Id, bill.PatientPayableTotal);
    }

    public async Task<PatientHmoEnrollmentDto> AddHmoEnrollmentAsync(Guid patientId, AddHmoEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var patientExists = await db.Patients.AnyAsync(x => x.Id == patientId, cancellationToken);
        if (!patientExists) throw new AppNotFoundException("Patient not found.");
        if (!await db.HmoCompanies.AnyAsync(x => x.Id == request.HmoCompanyId && x.IsActive, cancellationToken))
            throw new AppConflictException("HMO company is not active.");
        if (request.ValidTo is not null && request.ValidTo < request.ValidFrom)
            throw new AppConflictException("HMO validity dates are invalid.");
        if (request.IsPrimary)
        {
            var currentPrimary = await db.PatientHmoEnrollments
                .Where(x => x.PatientId == patientId && x.IsPrimary && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var primary in currentPrimary) primary.IsPrimary = false;
        }

        var enrollment = new PatientHmoEnrollment
        {
            PatientId = patientId,
            HmoCompanyId = request.HmoCompanyId,
            HmoMemberId = request.HmoMemberId.Trim(),
            PlanName = request.PlanName,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsPrimary = request.IsPrimary,
            CreatedBy = user.RequireId()
        };
        db.PatientHmoEnrollments.Add(enrollment);
        await db.SaveChangesAsync(cancellationToken);
        var hmo = await db.HmoCompanies.AsNoTracking().SingleAsync(x => x.Id == request.HmoCompanyId, cancellationToken);
        await audit.WriteAsync("patient.hmo-enrollment-added", nameof(PatientHmoEnrollment), enrollment.Id.ToString(), patientId,
            after: new { hmo.Name, enrollment.HmoMemberId, request.IsPrimary }, cancellationToken: cancellationToken);
        return new PatientHmoEnrollmentDto(enrollment.Id, enrollment.HmoCompanyId, hmo.Name, enrollment.HmoMemberId,
            enrollment.PlanName, enrollment.ValidFrom, enrollment.ValidTo, enrollment.IsPrimary, enrollment.IsActive);
    }

    public async Task<PatientHmoEnrollmentDto> AddHmoEnrollmentByNumberAsync(string patientNumber, AddHmoEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var patientId = await ResolveIdByNumberAsync(patientNumber, cancellationToken);
        return await AddHmoEnrollmentAsync(patientId, request, cancellationToken);
    }

    public async Task<PatientHmoEnrollmentDto> UpdateHmoEnrollmentByNumberAsync(string patientNumber, Guid enrollmentId, AddHmoEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var patientId = await ResolveIdByNumberAsync(patientNumber, cancellationToken);
        var enrollment = await db.PatientHmoEnrollments
            .SingleOrDefaultAsync(x => x.Id == enrollmentId && x.PatientId == patientId, cancellationToken)
            ?? throw new AppNotFoundException("HMO assignment not found for this patient.");
        if (!await db.HmoCompanies.AnyAsync(x => x.Id == request.HmoCompanyId && x.IsActive, cancellationToken))
            throw new AppConflictException("HMO company is not active.");
        if (request.ValidTo is not null && request.ValidTo < request.ValidFrom)
            throw new AppConflictException("HMO validity dates are invalid.");
        if (request.IsPrimary)
        {
            var primaries = await db.PatientHmoEnrollments
                .Where(x => x.PatientId == patientId && x.Id != enrollmentId && x.IsPrimary && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var primary in primaries) primary.IsPrimary = false;
        }
        enrollment.HmoCompanyId = request.HmoCompanyId;
        enrollment.HmoMemberId = request.HmoMemberId.Trim();
        enrollment.PlanName = request.PlanName;
        enrollment.ValidFrom = request.ValidFrom;
        enrollment.ValidTo = request.ValidTo;
        enrollment.IsPrimary = request.IsPrimary;
        await db.SaveChangesAsync(cancellationToken);
        var hmo = await db.HmoCompanies.AsNoTracking().SingleAsync(x => x.Id == request.HmoCompanyId, cancellationToken);
        await audit.WriteAsync("patient.hmo-enrollment-updated", nameof(PatientHmoEnrollment), enrollment.Id.ToString(), patientId,
            after: new { hmo.Name, enrollment.HmoMemberId, request.IsPrimary }, cancellationToken: cancellationToken);
        return new PatientHmoEnrollmentDto(enrollment.Id, enrollment.HmoCompanyId, hmo.Name, enrollment.HmoMemberId,
            enrollment.PlanName, enrollment.ValidFrom, enrollment.ValidTo, enrollment.IsPrimary, enrollment.IsActive);
    }

    private async Task ValidateEnrollmentsAsync(IReadOnlyList<CreateHmoEnrollmentRequest> enrollments, CancellationToken cancellationToken)
    {
        foreach (var enrollment in enrollments)
        {
            if (!await db.HmoCompanies.AnyAsync(x => x.Id == enrollment.HmoCompanyId && x.IsActive, cancellationToken))
                throw new AppConflictException($"HMO company {enrollment.HmoCompanyId} is not active.");
            if (enrollment.ValidTo is not null && enrollment.ValidTo < enrollment.ValidFrom)
                throw new AppConflictException("HMO validity dates are invalid.");
            if (string.IsNullOrWhiteSpace(enrollment.HmoMemberId))
                throw new AppConflictException("Every HMO enrollment requires an HMO member ID.");
        }
    }

    private async Task<int> GetRegistrationMonthsAsync(CancellationToken cancellationToken)
    {
        var setting = await db.ClinicSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == "registration.validity_months", cancellationToken);
        return setting is not null && int.TryParse(setting.ValueJson.Trim('"'), out var months) ? months : 24;
    }

    private static PatientDto Map(Patient patient) => new(
        patient.Id, patient.PatientNumber, patient.FullName, patient.FirstName, patient.LastName, patient.Sex, patient.DateOfBirth,
        patient.Phone, patient.Email, patient.Address, patient.StateOfOrigin, patient.HomeTown, patient.Allergy, patient.Occupation,
        patient.NextOfKinName, patient.NextOfKinPhone,
        patient.RegistrationDate, patient.CurrentRegistrationExpiresAt, patient.Status,
        patient.HmoEnrollments.OrderByDescending(x => x.IsPrimary).Select(x => new PatientHmoEnrollmentDto(
            x.Id, x.HmoCompanyId, x.HmoCompany?.Name ?? "HMO", x.HmoMemberId, x.PlanName,
            x.ValidFrom, x.ValidTo, x.IsPrimary, x.IsActive)).ToList());
}
