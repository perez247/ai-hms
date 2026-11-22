using Hms.Application.DTOs;
using Hms.Application.Features.Nursing;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

/// <summary>Nurse station: patient-level vitals, encounter services and shift handover.</summary>
[ApiController]
[Route("api/nurse")]
[Authorize]
public sealed class NurseController(IMediator mediator) : ControllerBase
{
    [HttpGet("patients/{patientNumber}/vitals")]
    [Authorize(Policy = Permissions.VitalsRead)]
    public async Task<IActionResult> Vitals(string patientNumber, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetNurseVitalsQuery(patientNumber, page, pageSize), cancellationToken));

    [HttpPost("patients/{patientNumber}/vitals")]
    [Authorize(Policy = Permissions.VitalsWrite)]
    public async Task<ActionResult<VitalDto>> RecordVitals(string patientNumber, RecordPatientVitalsRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RecordPatientVitalsCommand(request with { PatientNumber = patientNumber }), cancellationToken));

    [HttpGet("appointments")]
    [Authorize(Policy = Permissions.AppointmentsRead)]
    public async Task<IActionResult> ScheduledAppointments(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetScheduledAppointmentsQuery(), cancellationToken));

    [HttpGet("inpatients")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<IActionResult> Inpatients(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetInpatientsQuery(), cancellationToken));

    [HttpGet("patients/{patientNumber}/encounters")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<IActionResult> Encounters(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetOpenNurseEncountersQuery(patientNumber), cancellationToken));

    [HttpGet("handovers")]
    [Authorize(Policy = AppPolicies.NurseOnly)]
    public async Task<IActionResult> Handovers([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetShiftHandoversQuery(page, pageSize), cancellationToken));

    [HttpPost("handovers")]
    [Authorize(Policy = AppPolicies.NurseOnly)]
    public async Task<ActionResult<ShiftHandoverDto>> CreateHandover(CreateShiftHandoverRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateShiftHandoverCommand(request), cancellationToken));
}
