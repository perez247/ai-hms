using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record FitnessInvestigationResultRequest(string Key, string Value);
public sealed record FitnessInvestigationResultDto(string Key, string Value);

/// <summary>
/// Receptionist/Nurse basic capture. Patient number is optional; the full name is always required.
/// Payment type determines whether Finance settles individually or via the HMO batch screen.
/// </summary>
public sealed record CreateFitnessCertificateRequest(
    string? PatientNumber,
    string PatientFullName,
    int PatientAge,
    Sex PersonSex,
    string Phone,
    FitnessPaymentType PaymentType,
    Guid? HmoCompanyId,
    Guid? HmoEnrollmentId,
    DateOnly FitnessDate,
    DateOnly? ExpiresAt);

public sealed record UpdateFitnessClinicalInfoRequest(IReadOnlyList<FitnessInvestigationResultRequest> InvestigationResults);
public sealed record FitnessDecisionRequest(bool Approve, string? RejectionReason);
public sealed record FitnessPaymentRequest(bool Paid);
public sealed record FitnessBatchPaymentRequest(Guid HmoCompanyId, IReadOnlyList<Guid> CertificateIds, bool Paid);

public sealed record FitnessCertificateDto(
    Guid Id,
    string CertificateNumber,
    Guid? PatientId,
    string? PatientNumber,
    string PatientName,
    int PatientAge,
    Sex PersonSex,
    string Phone,
    FitnessPaymentType PaymentType,
    DateOnly FitnessDate,
    DateOnly? ExpiresAt,
    Guid? HmoCompanyId,
    string? HmoCompanyName,
    FitnessStatus Status,
    FitnessPaymentStatus PaymentStatus,
    string? RejectionReason,
    string? CertificateFileName,
    string? CertificateContentType,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? PaymentUpdatedAt,
    Guid? BillId,
    IReadOnlyList<FitnessInvestigationResultDto> InvestigationResults);

public sealed record FitnessCertificateFileDto(string FileName, string ContentType, Stream Content);
