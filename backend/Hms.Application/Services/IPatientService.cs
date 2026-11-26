using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IPatientService
{
    Task<PagedResult<PatientSearchItem>> SearchAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PatientDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientDto> GetByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<Guid> ResolveIdByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);
    Task<PatientDto> UpdateAsync(Guid patientId, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<RegistrationRenewalResult> RenewRegistrationAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientHmoEnrollmentDto> AddHmoEnrollmentAsync(Guid patientId, AddHmoEnrollmentRequest request, CancellationToken cancellationToken = default);
    Task<PatientHmoEnrollmentDto> AddHmoEnrollmentByNumberAsync(string patientNumber, AddHmoEnrollmentRequest request, CancellationToken cancellationToken = default);
    Task<PatientHmoEnrollmentDto> UpdateHmoEnrollmentByNumberAsync(string patientNumber, Guid enrollmentId, AddHmoEnrollmentRequest request, CancellationToken cancellationToken = default);
}
