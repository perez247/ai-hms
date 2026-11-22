using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record RecordPatientVitalsRequest(
    string PatientNumber,
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

public sealed record NursePrescriptionItemDto(
    Guid PrescriptionItemId,
    Guid? DispenseItemId,
    string MedicationName,
    string Dose,
    string Route,
    string Frequency,
    decimal QuantityPrescribed,
    decimal QuantityDispensed,
    bool CanAdminister);

public sealed record NurseEncounterDto(
    Guid Id,
    string EncounterNumber,
    string Type,
    string Status,
    string DoctorName,
    DateTimeOffset StartedAt,
    string? AdmissionStatus,
    IReadOnlyList<NursePrescriptionItemDto> Prescriptions);

public sealed record ShiftHandoverDto(Guid Id, Guid NurseId, string NurseName, DateOnly ShiftDate, string ContentHtml, DateTimeOffset CreatedAt);
public sealed record CreateShiftHandoverRequest(DateOnly? ShiftDate, string ContentHtml);

public sealed record NurseAppointmentDto(
    Guid Id,
    string AppointmentNumber,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset ScheduledAt,
    string Status,
    string? Reason);

public sealed record NurseInpatientDto(
    Guid AdmissionId,
    string AdmissionNumber,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    string AttendingDoctorName,
    string WardType,
    string? BedNumber,
    DateTimeOffset AdmittedAt,
    string Status,
    IReadOnlyList<Guid> EncounterIds);
