using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record PatientBasicProfileDto(PatientDto Patient, bool IsPrivate, bool CanUploadFiles);
public sealed record PatientVitalsPageDto(int Page, int PageSize, int Total, IReadOnlyList<VitalDto> Items);
public sealed record PatientEncounterSummaryDto(Guid Id, string EncounterNumber, string Type, string Status, string DoctorName, DateTimeOffset StartedAt, string? ChiefComplaint);
public sealed record PatientInvestigationDto(Guid Id, string Name, string Status, string EncounterNumber, DateTimeOffset OrderedAt, DateTimeOffset? PerformedAt, string? ReportText, string? ReferralDestination);
public sealed record PatientSurgeryDto(Guid Id, string Name, string Status, string EncounterNumber, DateTimeOffset ScheduledAt, DateTimeOffset? PerformedAt, string? ReportText);
public sealed record PatientFinanceDto(decimal TotalBilled, decimal TotalPaid, decimal Balance, string PayerType, IReadOnlyList<BillDto>? Bills);
public sealed record PatientFileDto(Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset CreatedAt, Guid? UploadedBy);
public sealed record PatientFileContentDto(string FileName, string ContentType, Stream Content);

/// <summary>
/// Permission-filtered patient profile. Clinical sections are null when the caller lacks the
/// corresponding existing permission; Finance is returned for finance roles or private patients.
/// </summary>
public sealed record PatientProfileDto(
    PatientDto Patient,
    bool IsPrivate,
    PatientVitalsPageDto? Vitals,
    IReadOnlyList<FitnessCertificateDto>? FitnessRecords,
    IReadOnlyList<PatientEncounterSummaryDto>? Encounters,
    IReadOnlyList<PatientInvestigationDto>? Investigations,
    IReadOnlyList<PatientSurgeryDto>? Surgeries,
    PatientFinanceDto? Finance,
    IReadOnlyList<PatientFileDto> Files,
    bool CanUploadFiles);

public sealed record StaffProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    StaffRole Role,
    bool IsActive,
    bool CanViewEmployment,
    DateOnly? HiredOn,
    string? EmployeeNumber,
    decimal? SalaryAmount,
    string? BankName,
    string? BankAccountName,
    string? BankAccountNumber);
