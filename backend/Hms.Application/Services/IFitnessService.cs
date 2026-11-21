using Hms.Application.Common;
using Hms.Application.DTOs;

namespace Hms.Application.Services;

/// <summary>Pre-procedure clearance lifecycle: nurse capture/commit, doctor decision, finance payment.</summary>
public interface IFitnessService
{
    Task<FitnessCertificateDto> CreateAsync(CreateFitnessCertificateRequest request, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto> GetAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto> UpdateClinicalInfoAsync(Guid certificateId, UpdateFitnessClinicalInfoRequest request, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto> UploadCertificateAsync(Guid certificateId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task<FitnessCertificateFileDto> DownloadCertificateAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto> SubmitAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto> DecideAsync(Guid certificateId, FitnessDecisionRequest request, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto> SetPaymentAsync(Guid certificateId, FitnessPaymentRequest request, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto[]> SetHmoBatchPaymentAsync(FitnessBatchPaymentRequest request, CancellationToken cancellationToken = default);
    Task<FitnessCertificateDto> MarkPaidAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<PagedResult<FitnessCertificateDto>> SearchAsync(DateOnly? from, DateOnly? to, Guid? hmoCompanyId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FitnessCertificateDto>> GetForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(DateOnly? from, DateOnly? to, Guid? hmoCompanyId, string? search, CancellationToken cancellationToken = default);
}
