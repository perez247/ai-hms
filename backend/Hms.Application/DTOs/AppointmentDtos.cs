using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record CreateAppointmentRequest(
    string PatientId,
    Guid DoctorId,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string? Reason);

public sealed record AppointmentDto(
    Guid Id,
    string AppointmentNumber,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string? Reason,
    AppointmentStatus Status,
    Guid? BillId);
