using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record CreateAppointmentRequest(
    string PatientId,
    Guid DoctorId,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string? Reason,
    FundingSource FundingSource = FundingSource.Private,
    Guid? HmoEnrollmentId = null);

/// <summary>An appointment that still blocks a new booking: its visit bill has not been paid or invoiced to an HMO.</summary>
public sealed record OngoingAppointmentDto(
    Guid Id,
    string AppointmentNumber,
    DateTimeOffset ScheduledAt,
    AppointmentStatus Status,
    BillStatus? BillStatus);

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
    Guid? BillId,
    FundingSource FundingSource = FundingSource.Private,
    Guid? HmoEnrollmentId = null);

/// <summary>Reason- and payer-aware appointment fee resolved from the stock/price catalogue.</summary>
public sealed record AppointmentPriceQuoteDto(
    string Reason,
    FundingSource FundingSource,
    Guid? HmoEnrollmentId,
    string PayerName,
    decimal UnitPrice,
    int CoveragePercent,
    decimal HmoAmount,
    decimal PatientAmount,
    bool IsConfigured);
