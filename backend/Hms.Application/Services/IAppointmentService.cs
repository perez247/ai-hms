using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IAppointmentService
{
    Task<PagedResult<AppointmentDto>> SearchAsync(DateOnly? date, Guid? doctorId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> CheckInAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<OngoingAppointmentDto?> GetOngoingForPatientAsync(string patientNumber, CancellationToken cancellationToken = default);
}
