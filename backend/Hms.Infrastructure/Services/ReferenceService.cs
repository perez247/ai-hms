using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Small lookup projections used by forms (HMOs, doctors, enrollments).</summary>
public sealed class ReferenceService(HmsDbContext db) : IReferenceService
{
    public async Task<IReadOnlyList<HmoOptionDto>> GetHmosAsync(CancellationToken cancellationToken = default) =>
        await db.HmoCompanies.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new HmoOptionDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DoctorOptionDto>> GetDoctorsAsync(CancellationToken cancellationToken = default) =>
        await db.Users.AsNoTracking().Where(x => x.IsActive && x.StaffRole == StaffRole.Doctor)
            .OrderBy(x => x.FullName).Select(x => new DoctorOptionDto(x.Id, x.FullName)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HmoEnrollmentOptionDto>> GetPatientHmoEnrollmentsAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await db.PatientHmoEnrollments.AsNoTracking().Include(x => x.HmoCompany)
            .Where(x => x.PatientId == patientId && x.IsActive)
            .OrderByDescending(x => x.IsPrimary)
            .Select(x => new HmoEnrollmentOptionDto(x.Id, x.HmoCompanyId, x.HmoCompany!.Name, x.HmoMemberId, x.PlanName, x.IsPrimary))
            .ToListAsync(cancellationToken);
}
