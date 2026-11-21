using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record CreateHmoEnrollmentRequest(
    Guid HmoCompanyId,
    string HmoMemberId,
    string? PlanName,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsPrimary);

public sealed record CreatePatientRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    Sex Sex,
    DateOnly DateOfBirth,
    string? Phone,
    string? Email,
    string? Address,
    string? NextOfKinName,
    string? NextOfKinPhone,
    IReadOnlyList<CreateHmoEnrollmentRequest>? HmoEnrollments);

public sealed record PatientHmoEnrollmentDto(
    Guid Id,
    Guid HmoCompanyId,
    string HmoName,
    string HmoMemberId,
    string? PlanName,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsPrimary,
    bool IsActive);

public sealed record PatientDto(
    Guid Id,
    string PatientNumber,
    string FullName,
    Sex Sex,
    DateOnly DateOfBirth,
    string? Phone,
    string? Email,
    string? Address,
    string? NextOfKinName,
    string? NextOfKinPhone,
    DateOnly RegistrationDate,
    DateOnly CurrentRegistrationExpiresAt,
    PatientStatus Status,
    IReadOnlyList<PatientHmoEnrollmentDto> HmoEnrollments);

public sealed record PatientSearchItem(
    Guid Id,
    string PatientNumber,
    string FullName,
    string? Phone,
    DateOnly CurrentRegistrationExpiresAt,
    PatientStatus Status);

public sealed record AddHmoEnrollmentRequest(
    Guid HmoCompanyId,
    string HmoMemberId,
    string? PlanName,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool IsPrimary);

public sealed record RegistrationRenewalResult(Guid PatientId, DateOnly PeriodEnd, Guid BillId, decimal FeeAmount);
