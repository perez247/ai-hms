using Hms.Application.DTOs;

namespace Hms.Application.Services;

public interface IReferenceService
{
    Task<IReadOnlyList<HmoOptionDto>> GetHmosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorOptionDto>> GetDoctorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HmoEnrollmentOptionDto>> GetPatientHmoEnrollmentsAsync(Guid patientId, CancellationToken cancellationToken = default);
}
