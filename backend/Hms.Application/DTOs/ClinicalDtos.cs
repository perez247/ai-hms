using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record CreateOutpatientEncounterRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid? AppointmentId,
    bool IsEmergency,
    Guid? HmoEnrollmentId,
    string? PresentingComplaint);

public sealed record CreateAdmissionRequest(
    Guid PatientId,
    Guid DoctorId,
    WardType WardType,
    string? BedNumber,
    Guid? HmoEnrollmentId,
    string? PresentingComplaint,
    DateTimeOffset? AdmittedAt);

public sealed record CreateInpatientEncounterRequest(
    Guid AdmissionId,
    Guid DoctorId,
    string? PresentingComplaint,
    string? HistoryOfPresentingComplaint = null,
    string? PastMedicalSurgicalHistory = null,
    string? FamilyHistory = null,
    string? Diagnosis = null,
    string? ClerkingDetails = null,
    IReadOnlyList<PrescribeOrderRequest>? Medications = null);

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
    string PatientNumber,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    EncounterType Type,
    EncounterStatus Status,
    bool IsEmergency,
    DateTimeOffset StartedAt,
    DateTimeOffset? ClosedAt,
    Guid? AdmissionId,
    Guid? BillId,
    string? PresentingComplaint,
    string? HistoryOfPresentingComplaint,
    string? PastMedicalSurgicalHistory,
    string? FamilyHistory,
    IReadOnlyList<ExaminationItemDto> Examination,
    string? Diagnosis,
    string? Disposition,
    IReadOnlyList<VitalDto> Vitals,
    IReadOnlyList<PrescriptionSummaryDto> Prescriptions,
    IReadOnlyList<InvestigationSummaryDto> Investigations,
    IReadOnlyList<SurgerySummaryDto> Surgeries,
    FundingSource FundingSource = FundingSource.Private,
    Guid? HmoEnrollmentId = null,
    IReadOnlyList<ServiceRequestSummaryDto>? ServiceRequests = null,
    IReadOnlyList<EncounterHistoryItemDto>? PriorEncounters = null,
    int? PresentingComplaintDurationDays = null,
    ClerkingDetailsDto? Clerking = null);

public sealed record ServiceRequestSummaryDto(
    Guid Id, string RequestNumber, StockItemType Kind, string ItemName,
    ServiceRequestStatus Status, ServiceResultStatus? ResultStatus,
    string? ResultText, string? Findings, decimal UnitPrice, FundingSource FundingSource,
    bool? AbnormalFlag = null, DateTimeOffset? PerformedAt = null,
    string? Priority = null, string? ClinicalNote = null, Guid StockItemId = default);

/// <summary>Ancillary clerking fields that do not have dedicated encounter columns.</summary>
public sealed record ClerkingDetailsDto(
    string? RosNotes,
    IReadOnlyList<string>? Allergies,
    string? GeneralObservation,
    IReadOnlyList<PointOfCareItemDto>? PointOfCare,
    string? ClinicalImpression,
    string? PlanAdvice,
    DateOnly? FollowUpDate,
    string? ReferralDetails);

public sealed record PointOfCareItemDto(string Key, string? Value);

/// <summary>Read-only summary of an earlier encounter for the patient history accordion.</summary>
public sealed record EncounterHistoryItemDto(
    Guid Id,
    string EncounterNumber,
    EncounterType Type,
    EncounterStatus Status,
    string DoctorName,
    DateTimeOffset StartedAt,
    string? PresentingComplaint,
    string? Diagnosis,
    string? Disposition,
    string? PastMedicalSurgicalHistory,
    string? FamilyHistory,
    int? PresentingComplaintDurationDays = null,
    string? HistoryOfPresentingComplaint = null,
    IReadOnlyList<ExaminationItemDto>? Examination = null,
    ClerkingDetailsDto? Clerking = null,
    IReadOnlyList<PrescriptionItemSummaryDto>? Medications = null,
    IReadOnlyList<EncounterPlanOrderDto>? Orders = null,
    int ResultCount = 0);

/// <summary>One test/procedure order shown in a previous encounter's plan.</summary>
public sealed record EncounterPlanOrderDto(
    Guid Id,
    string ItemName,
    StockItemType Kind,
    ServiceRequestStatus Status,
    string? Priority = null,
    string? ClinicalNote = null);

public sealed record ExaminationItemDto(string Name, string? Result, bool? Normal = null);

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

public sealed record PrescriptionSummaryDto(
    Guid Id,
    PrescriptionStatus Status,
    DateTimeOffset CreatedAt,
    int ItemCount,
    IReadOnlyList<PrescriptionItemSummaryDto>? Items = null);

public sealed record PrescriptionItemSummaryDto(
    Guid Id,
    string MedicationName,
    string Dose,
    string Route,
    string Frequency,
    int DurationDays,
    decimal QuantityPrescribed,
    decimal QuantityDispensed,
    string? Instructions,
    Guid StockItemId = default);
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

/// <summary>A patient waiting for a doctor to open an encounter.</summary>
public sealed record DoctorQueuePatientDto(
    Guid SourceId,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    string ReferenceNumber,
    string SourceKind,
    DateTimeOffset Since,
    string? HmoName,
    bool HasOpenEncounter,
    Guid? OpenEncounterId);

public sealed record DoctorQueueDto(
    IReadOnlyList<DoctorQueuePatientDto> AwaitingOutpatients,
    IReadOnlyList<DoctorQueuePatientDto> ActiveInpatients);

/// <summary>One row of the doctor's combined outpatient/inpatient encounter worklist.</summary>
public sealed record EncounterListItemDto(
    Guid Id,
    string EncounterNumber,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    string DoctorName,
    EncounterType Type,
    EncounterStatus Status,
    bool IsEmergency,
    DateTimeOffset StartedAt,
    Guid? AppointmentId,
    string? AppointmentNumber,
    Guid? AdmissionId,
    string? AdmissionNumber,
    string? PresentingComplaint);

/// <summary>Structured clerking payload persisted on an encounter. Text fields are capped at 1000
/// characters; the examination list allows at most 20 named findings.</summary>
public sealed record SaveEncounterRequest(
    string? PresentingComplaint,
    string? HistoryOfPresentingComplaint,
    string? PastMedicalSurgicalHistory,
    string? FamilyHistory,
    string? Diagnosis,
    string? Disposition,
    IReadOnlyList<ExaminationItemRequest>? Examination,
    bool Complete,
    IReadOnlyList<PrescribeOrderRequest>? Medications = null,
    IReadOnlyList<ServiceOrderRequest>? Tests = null,
    IReadOnlyList<ServiceOrderRequest>? Procedures = null,
    int? PresentingComplaintDurationDays = null,
    ClerkingDetailsDto? Clerking = null);

public sealed record PrescribeOrderRequest(
    Guid StockItemId, string Dose, string Route, string Frequency, int DurationDays, decimal Quantity,
    string? Instructions = null, Guid? Id = null);

public sealed record ServiceOrderRequest(Guid StockItemId, string? Priority, string? ClinicalNote, Guid? Id = null);

public sealed record ExaminationItemRequest(string Name, string? Result, bool? Normal = null);
