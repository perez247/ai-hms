using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record SavePriorAuthorizationRequest(
    Guid HmoCompanyId,
    Guid HmoEnrollmentId,
    Guid? EncounterId,
    Guid? AdmissionId,
    string PaCode,
    DateTimeOffset? ExpiresAt,
    decimal? ApprovedAmount,
    string? Notes);

public sealed record PriorAuthorizationDto(
    Guid Id,
    Guid PatientId,
    Guid HmoCompanyId,
    string HmoName,
    Guid? EncounterId,
    Guid? AdmissionId,
    string PaCode,
    PriorAuthorizationStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ExpiresAt,
    decimal? ApprovedAmount,
    string? Notes);

public sealed record HmoLineCoverageRequest(
    Guid BillLineId,
    bool IsCovered,
    decimal HmoAmount,
    string? ReferralDestination);

public sealed record UpdateHmoCoverageRequest(IReadOnlyList<HmoLineCoverageRequest> Lines);
