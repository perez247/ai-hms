using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Infrastructure.Services;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Coordinates appointment booking/check-in and the appointment-fee bill.</summary>
public sealed class AppointmentService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    INumberGenerator numbers,
    IBillingService billing,
    IFundingService funding,
    IAppointmentPricingService pricing) : IAppointmentService
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
            .Where(x => x.Kind == BillKind.Visit && x.AppointmentId != null && ids.Contains(x.AppointmentId.Value) && x.Status != BillStatus.Void)
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
        if (!AppointmentReasons.IsValid(request.Reason))
            throw new AppConflictException("Appointment reason must be Consultation or Emergency.");

        // A patient may hold only one open appointment. The appointment closes once its visit bill
        // has been paid, approved, or invoiced/routed to the HMO; a missing bill means it is still open.
        var ongoing = await OngoingAppointments(patient.Id).FirstOrDefaultAsync(cancellationToken);
        if (ongoing is not null)
            throw new AppConflictException(
                $"This patient already has an ongoing appointment ({ongoing.AppointmentNumber}, " +
                $"{ongoing.ScheduledAt:dd MMM yyyy HH:mm}, {ongoing.Status}). " +
                "The visit bill must be paid or invoiced to the HMO before a new appointment can be booked.");

        var serviceDate = DateOnly.FromDateTime(request.ScheduledAt.UtcDateTime);
        var fundingContext = await funding.VerifyAsync(request.FundingSource, request.HmoEnrollmentId, patient.Id, serviceDate, cancellationToken);
        // Fail before booking when the reason/payer has no configured price.
        await pricing.QuoteAsync(request.Reason, fundingContext.Source, fundingContext.HmoEnrollmentId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var appointment = new Appointment
        {
            AppointmentNumber = await numbers.NextAsync("appointment_number_seq", "APT", cancellationToken),
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Reason = request.Reason,
            FundingSource = request.FundingSource,
            HmoEnrollmentId = fundingContext.HmoEnrollmentId,
            CreatedBy = user.RequireId()
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        await audit.WriteAsync("appointment.created", nameof(Appointment), appointment.Id.ToString(), patient.Id,
            after: new { appointment.AppointmentNumber, appointment.ScheduledAt, doctor.FullName }, cancellationToken: cancellationToken);
        return Map(appointment, doctor.FullName, null);
    }

    public async Task<AppointmentDto> CheckInAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await db.Appointments.Include(x => x.Patient)
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken)
            ?? throw new AppNotFoundException("Appointment not found.");
        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
            throw new AppConflictException("This appointment cannot be checked in.");
        var fundingContext = await funding.VerifyAsync(appointment.FundingSource, appointment.HmoEnrollmentId,
            appointment.PatientId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        appointment.FundingSource = fundingContext.Source;
        appointment.HmoEnrollmentId = fundingContext.HmoEnrollmentId;
        appointment.Status = AppointmentStatus.CheckedIn;
        appointment.CheckedInAt = DateTimeOffset.UtcNow;
        appointment.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var doctorName = await db.Users.AsNoTracking().Where(x => x.Id == appointment.DoctorId).Select(x => x.FullName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Unknown";
        var visitBill = await billing.GetOrCreateVisitBillAsync(appointment, cancellationToken);
        var billId = (Guid?)visitBill.Id;
        await audit.WriteAsync("appointment.checked-in", nameof(Appointment), appointment.Id.ToString(), appointment.PatientId, cancellationToken: cancellationToken);
        return Map(appointment, doctorName, billId);
    }

    public async Task<OngoingAppointmentDto?> GetOngoingForPatientAsync(string patientNumber,
        CancellationToken cancellationToken = default)
    {
        var patientId = await db.Patients.AsNoTracking()
            .Where(x => x.PatientNumber == patientNumber).Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        return await OngoingAppointments(patientId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Open = not cancelled/no-show and the linked visit bill is still Draft/Committed (or absent).
    /// Paid, approved, HMO-pending and HMO-approved bills close the appointment.
    /// </summary>
    private IQueryable<OngoingAppointmentDto> OngoingAppointments(Guid patientId) =>
        db.Appointments.AsNoTracking()
            .Where(x => x.PatientId == patientId
                && x.Status != AppointmentStatus.Cancelled
                && x.Status != AppointmentStatus.NoShow)
            .Where(x => !db.Bills.Any(b => b.AppointmentId == x.Id && b.Status != BillStatus.Void
                && (b.Status == BillStatus.Paid || b.Status == BillStatus.Approved
                    || b.Status == BillStatus.HmoPending || b.Status == BillStatus.HmoApproved)))
            .OrderByDescending(x => x.ScheduledAt)
            .Select(x => new OngoingAppointmentDto(x.Id, x.AppointmentNumber, x.ScheduledAt, x.Status,
                db.Bills.Where(b => b.AppointmentId == x.Id && b.Status != BillStatus.Void)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => (BillStatus?)b.Status).FirstOrDefault()));

    private static AppointmentDto Map(Appointment appointment, string doctorName, Guid? billId) => new(
        appointment.Id, appointment.AppointmentNumber, appointment.PatientId, appointment.Patient?.PatientNumber ?? "—", appointment.Patient?.FullName ?? "Unknown",
        appointment.DoctorId, doctorName, appointment.ScheduledAt,
        appointment.DurationMinutes, appointment.Reason, appointment.Status, billId,
        appointment.FundingSource, appointment.HmoEnrollmentId);
}
