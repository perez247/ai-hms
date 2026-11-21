using Hms.Application.DTOs;

namespace Hms.Application.Services;

/// <summary>Permission-filtered reads and file operations for the patient profile screen.</summary>
public interface IPatientProfileService
{
    Task<PatientProfileDto> GetAsync(Guid patientId, int vitalsPage, int vitalsPageSize, CancellationToken cancellationToken = default);
    Task<PatientVitalsPageDto> GetVitalsAsync(Guid patientId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PatientFileDto> UploadFileAsync(Guid patientId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task<PatientFileContentDto> DownloadFileAsync(Guid patientId, Guid fileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientHmoEnrollmentDto>> GetHmosByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<PatientBasicProfileDto> GetBasicByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<PatientVitalsPageDto> GetVitalsByNumberAsync(string patientNumber, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FitnessCertificateDto>> GetFitnessByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientEncounterSummaryDto>> GetEncountersByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientInvestigationDto>> GetInvestigationsByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientSurgeryDto>> GetSurgeriesByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<PatientFinanceDto?> GetFinanceByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientFileDto>> GetFilesByNumberAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<PatientFileDto> UploadFileByNumberAsync(string patientNumber, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task<PatientFileContentDto> DownloadFileByNumberAsync(string patientNumber, Guid fileId, CancellationToken cancellationToken = default);
}

public interface IStaffProfileService
{
    Task<StaffProfileDto> GetAsync(Guid? staffId, CancellationToken cancellationToken = default);
}
