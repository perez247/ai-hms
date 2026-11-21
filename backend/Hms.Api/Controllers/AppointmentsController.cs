using Hms.Application.DTOs;
using Hms.Application.Features.Appointments;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
/// <summary>Appointment booking/check-in; booking also creates the appointment-fee bill.</summary>
public sealed class AppointmentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.AppointmentsRead)]
    public async Task<IActionResult> Search([FromQuery] DateOnly? date, [FromQuery] Guid? doctorId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchAppointmentsQuery(date, doctorId, page, pageSize), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.AppointmentsCreate)]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateAppointmentCommand(request), cancellationToken));

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Policy = Permissions.AppointmentsCreate)]
    public async Task<ActionResult<AppointmentDto>> CheckIn(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CheckInAppointmentCommand(id), cancellationToken));
}
