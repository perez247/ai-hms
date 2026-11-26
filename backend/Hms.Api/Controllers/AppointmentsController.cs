using Hms.Application.DTOs;
using Hms.Application.Features.Appointments;
using Hms.Application.Security;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
/// <summary>Appointment booking/check-in; booking also creates the appointment-fee bill.</summary>
public sealed class AppointmentsController(IMediator mediator, IAppointmentPricingService pricing) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.AppointmentsRead)]
    public async Task<IActionResult> Search([FromQuery] DateOnly? date, [FromQuery] Guid? doctorId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchAppointmentsQuery(date, doctorId, page, pageSize), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.AppointmentsCreate)]
    [Authorize(Policy = Permissions.FundingVerify)]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateAppointmentCommand(request), cancellationToken));

    [HttpGet("ongoing")]
    [Authorize(Policy = Permissions.AppointmentsRead)]
    public async Task<IActionResult> Ongoing([FromQuery] string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetOngoingAppointmentQuery(patientNumber), cancellationToken));

    [HttpGet("quote")]
    [Authorize(Policy = Permissions.AppointmentsRead)]
    public async Task<ActionResult<AppointmentPriceQuoteDto>> Quote([FromQuery] string? reason,
        [FromQuery] FundingSource fundingSource = FundingSource.Private, [FromQuery] Guid? hmoEnrollmentId = null,
        CancellationToken cancellationToken = default) =>
        Ok(await pricing.QuoteAsync(reason, fundingSource, hmoEnrollmentId, cancellationToken));

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Policy = Permissions.AppointmentsCreate)]
    public async Task<ActionResult<AppointmentDto>> CheckIn(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CheckInAppointmentCommand(id), cancellationToken));
}
