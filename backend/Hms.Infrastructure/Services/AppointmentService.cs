using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Coordinates appointment booking/check-in and the appointment-fee bill.</summary>
public sealed class AppointmentService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    INumberGenerator numbers,
    IBillingService billing) : IAppointmentService
{
    public async Task<PagedResult<AppointmentDto>> SearchAsync(DateOnly? date, Guid? doctorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Appointments.AsNoTracking()
            .Include(x => x.Patient).AsQueryable();
        if (date is not null)
        {
            var start = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = start.AddDays(1);
            query = query.Where(x => x.ScheduledAt >= start && x.ScheduledAt < end);
        }
        if (doctorId is not null) query = query.Where(x => x.DoctorId == doctorId);
        var total = await query.CountAsync(cancellationToken);
        var appointments = await query.OrderBy(x => x.ScheduledAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = appointments.Select(x => x.Id).ToList();
        var doctorIds = appointments.Select(x => x.DoctorId).Distinct().ToList();
        var doctorNames = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        var bills = await db.Bills.AsNoTracking()
            .Where(x => x.Kind == BillKind.Appointment && x.AppointmentId != null && ids.Contains(x.AppointmentId.Value) && x.Status != BillStatus.Void)
            .Select(x => new { x.AppointmentId, x.Id })
            .ToDictionaryAsync(x => x.AppointmentId!.Value, x => x.Id, cancellationToken);
        return new PagedResult<AppointmentDto>(appointments.Select(x => Map(x, doctorNames.GetValueOrDefault(x.DoctorId, "Unknown"), bills.GetValueOrDefault(x.Id))).ToList(), page, pageSize, total);
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(x => x.PatientNumber == request.PatientId, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        if (patient.Status == PatientStatus.Inactive) throw new AppConflictException("Inactive patients cannot book appointments.");
        var doctor = await db.Users.SingleOrDefaultAsync(x => x.Id == request.DoctorId && x.StaffRole == StaffRole.Doctor && x.IsActive, cancellationToken)
            ?? throw new AppConflictException("Selected doctor is not an active doctor account.");
        if (request.ScheduledAt <= DateTimeOffset.UtcNow)
            throw new AppConflictException("Appointment must be scheduled in the future.");
        if (request.DurationMinutes <= 0) throw new AppConflictException("Duration must be greater than zero.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var appointment = new Appointment
        {
            AppointmentNumber = await numbers.NextAsync("appointment_number_seq", "APT", cancellationToken),
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Reason = request.Reason,
            CreatedBy = user.RequireId()
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        var bill = await billing.CreateAdministrativeBillAsync(
            patient.Id, BillKind.Appointment, FeeCode.Appointment, appointment.Id,
            ChargeSourceType.Appointment, $"Appointment {appointment.AppointmentNumber}", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await audit.WriteAsync("appointment.created", nameof(Appointment), appointment.Id.ToString(), patient.Id,
            after: new { appointment.AppointmentNumber, appointment.ScheduledAt, doctor.FullName }, cancellationToken: cancellationToken);
        return Map(appointment, doctor.FullName, bill.Id);
    }

    public async Task<AppointmentDto> CheckInAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await db.Appointments.Include(x => x.Patient)
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken)
            ?? throw new AppNotFoundException("Appointment not found.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
            throw new AppConflictException("This appointment cannot be checked in.");
        appointment.Status = AppointmentStatus.CheckedIn;
        appointment.CheckedInAt = DateTimeOffset.UtcNow;
        appointment.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var doctorName = await db.Users.AsNoTracking().Where(x => x.Id == appointment.DoctorId).Select(x => x.FullName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Unknown";
        var billId = await db.Bills.AsNoTracking()
            .Where(x => x.Kind == BillKind.Appointment && x.AppointmentId == appointment.Id && x.Status != BillStatus.Void)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        await audit.WriteAsync("appointment.checked-in", nameof(Appointment), appointment.Id.ToString(), appointment.PatientId, cancellationToken: cancellationToken);
        return Map(appointment, doctorName, billId);
    }

    private static AppointmentDto Map(Appointment appointment, string doctorName, Guid? billId) => new(
        appointment.Id, appointment.AppointmentNumber, appointment.PatientId, appointment.Patient?.PatientNumber ?? "—", appointment.Patient?.FullName ?? "Unknown",
        appointment.DoctorId, doctorName, appointment.ScheduledAt,
        appointment.DurationMinutes, appointment.Reason, appointment.Status, billId);
}
