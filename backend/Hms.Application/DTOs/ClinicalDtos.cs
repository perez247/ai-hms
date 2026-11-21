using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record CreateOutpatientEncounterRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid? AppointmentId,
    bool IsEmergency,
    Guid? HmoEnrollmentId,
    string? ChiefComplaint);

public sealed record CreateAdmissionRequest(
    Guid PatientId,
    Guid DoctorId,
    WardType WardType,
    string? BedNumber,
    Guid? HmoEnrollmentId,
    string? ChiefComplaint,
    DateTimeOffset? AdmittedAt);

public sealed record CreateInpatientEncounterRequest(Guid AdmissionId, Guid DoctorId, string? ChiefComplaint);

public sealed record RecordVitalsRequest(
    Guid EncounterId,
    decimal? TemperatureC,
    int? Systolic,
    int? Diastolic,
    int? Pulse,
    int? RespiratoryRate,
    int? Spo2,
    decimal? WeightKg,
    decimal? HeightCm,
    int? PainScore,
    string? Notes);

public sealed record PrescriptionItemRequest(
    Guid MedicationId,
    string Dose,
    string Route,
    string Frequency,
    int DurationDays,
    decimal Quantity,
    bool IsPrn,
    string? Instructions);

public sealed record CreatePrescriptionRequest(
    Guid EncounterId,
    string? Notes,
    IReadOnlyList<PrescriptionItemRequest> Items);

public sealed record CreateInvestigationRequest(Guid EncounterId, Guid CatalogItemId);

public sealed record CompleteInvestigationRequest(
    string ReportText,
    string? ReportFileRef,
    string? ReferralDestination,
    IReadOnlyList<Guid> ParticipantUserIds);

public sealed record CreateSurgeryRequest(
    Guid EncounterId,
    Guid CatalogItemId,
    DateTimeOffset ScheduledAt,
    string? Theater);

public sealed record CompleteSurgeryRequest(
    string ReportText,
    string? ReportFileRef,
    IReadOnlyList<Guid> ParticipantUserIds);

public sealed record RecordAdministrationRequest(
    Guid DispenseItemId,
    decimal DoseGiven,
    string Route,
    string? BatchNumber,
    string? Notes);

public sealed record RequestDischargeRequest(Guid AdmissionId, string DischargeSummary);

public sealed record EncounterDto(
    Guid Id,
    string EncounterNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    EncounterType Type,
    EncounterStatus Status,
    bool IsEmergency,
    DateTimeOffset StartedAt,
    Guid? AdmissionId,
    Guid? BillId,
    IReadOnlyList<VitalDto> Vitals,
    IReadOnlyList<PrescriptionSummaryDto> Prescriptions,
    IReadOnlyList<InvestigationSummaryDto> Investigations,
    IReadOnlyList<SurgerySummaryDto> Surgeries);

public sealed record VitalDto(
    Guid Id,
    DateTimeOffset RecordedAt,
    decimal? TemperatureC,
    int? Systolic,
    int? Diastolic,
    int? Pulse,
    int? RespiratoryRate,
    int? Spo2,
    decimal? WeightKg,
    decimal? HeightCm,
    int? PainScore,
    string? Notes);

public sealed record PrescriptionSummaryDto(Guid Id, PrescriptionStatus Status, DateTimeOffset CreatedAt, int ItemCount);
public sealed record InvestigationSummaryDto(Guid Id, string Name, InvestigationStatus Status, DateTimeOffset OrderedAt);
public sealed record SurgerySummaryDto(Guid Id, string Name, SurgeryStatus Status, DateTimeOffset ScheduledAt);

public sealed record AdmissionDto(
    Guid Id,
    string AdmissionNumber,
    Guid PatientId,
    string PatientName,
    Guid AttendingDoctorId,
    string AttendingDoctorName,
    WardType WardType,
    string? BedNumber,
    DateTimeOffset AdmittedAt,
    DateTimeOffset? DischargedAt,
    AdmissionStatus Status,
    string? DischargeSummary,
    Guid? BillId,
    IReadOnlyList<Guid> EncounterIds);

public sealed record DischargeRequestResult(Guid AdmissionId, Guid BillId, decimal PatientPayable, decimal HmoPayable, string BillStatus);
