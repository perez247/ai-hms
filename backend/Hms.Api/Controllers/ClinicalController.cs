using Hms.Application.DTOs;
using Hms.Application.Features.Clinical;
using Hms.Application.Security;
using Hms.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/clinical")]
[Authorize]
/// <summary>Doctor/Nurse clinical writes and encounter/admission reads.</summary>
public sealed class ClinicalController(IMediator mediator) : ControllerBase
{
    [HttpPost("encounters/outpatient")]
    [Authorize(Policy = Permissions.EncountersWrite)]
    public async Task<ActionResult<EncounterDto>> CreateOutpatient(CreateOutpatientEncounterRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateOutpatientEncounterCommand(request), cancellationToken));

    [HttpPost("admissions")]
    [Authorize(Policy = Permissions.EncountersWrite)]
    public async Task<ActionResult<AdmissionDto>> CreateAdmission(CreateAdmissionRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateAdmissionCommand(request), cancellationToken));

    [HttpPost("admissions/{id:guid}/encounters")]
    [Authorize(Policy = Permissions.EncountersWrite)]
    public async Task<ActionResult<EncounterDto>> AddInpatientEncounter(Guid id, CreateInpatientEncounterRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new AddInpatientEncounterCommand(request with { AdmissionId = id }), cancellationToken));

    [HttpPost("vitals")]
    [Authorize(Policy = Permissions.VitalsWrite)]
    public async Task<ActionResult<VitalDto>> RecordVitals(RecordVitalsRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RecordVitalsCommand(request), cancellationToken));

    [HttpGet("doctor-queue")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<ActionResult<DoctorQueueDto>> GetDoctorQueue(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetDoctorQueueQuery(), cancellationToken));

    [HttpGet("encounters")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<IActionResult> SearchEncounters([FromQuery] EncounterType? type, [FromQuery] EncounterStatus? status,
        [FromQuery] DateOnly? date, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SearchEncountersQuery(type, status, date, search), cancellationToken));

    [HttpGet("encounters/{id:guid}")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<ActionResult<EncounterDto>> GetEncounter(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetEncounterQuery(id), cancellationToken));

    [HttpGet("encounters/{id:guid}/results")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<IActionResult> GetEncounterResults(Guid id, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetEncounterResultsQuery(id, page, pageSize), cancellationToken));

    [HttpPut("encounters/{id:guid}/clerking")]
    [Authorize(Policy = Permissions.EncountersSign)]
    public async Task<ActionResult<EncounterDto>> SaveEncounter(Guid id, SaveEncounterRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveEncounterCommand(id, request), cancellationToken));

    [HttpGet("admissions/{id:guid}")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<ActionResult<AdmissionDto>> GetAdmission(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetAdmissionQuery(id), cancellationToken));

    [HttpPost("prescriptions")]
    [Authorize(Policy = Permissions.PrescriptionsWrite)]
    public async Task<ActionResult<PrescriptionSummaryDto>> CreatePrescription(CreatePrescriptionRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreatePrescriptionCommand(request), cancellationToken));

    [HttpPost("investigations")]
    [Authorize(Policy = Permissions.InvestigationsWrite)]
    public async Task<ActionResult<InvestigationSummaryDto>> CreateInvestigation(CreateInvestigationRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateInvestigationCommand(request), cancellationToken));

    [HttpPost("investigations/{id:guid}/complete")]
    [Authorize(Policy = Permissions.InvestigationsWrite)]
    public async Task<ActionResult<InvestigationSummaryDto>> CompleteInvestigation(Guid id, CompleteInvestigationRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CompleteInvestigationCommand(id, request), cancellationToken));

    [HttpPost("surgeries")]
    [Authorize(Policy = Permissions.SurgeriesWrite)]
    public async Task<ActionResult<SurgerySummaryDto>> CreateSurgery(CreateSurgeryRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateSurgeryCommand(request), cancellationToken));

    [HttpPost("surgeries/{id:guid}/complete")]
    [Authorize(Policy = Permissions.SurgeriesWrite)]
    public async Task<ActionResult<SurgerySummaryDto>> CompleteSurgery(Guid id, CompleteSurgeryRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CompleteSurgeryCommand(id, request), cancellationToken));

    [HttpPost("administrations")]
    [Authorize(Policy = Permissions.MedicationAdminister)]
    public async Task<ActionResult<object>> RecordAdministration(RecordAdministrationRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new RecordAdministrationCommand(request), cancellationToken);
        return Ok(new { id });
    }

    [HttpPost("admissions/{id:guid}/request-discharge")]
    [Authorize(Policy = Permissions.DischargeInitiate)]
    public async Task<ActionResult<DischargeRequestResult>> RequestDischarge(Guid id, RequestDischargeRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RequestDischargeCommand(request with { AdmissionId = id }), cancellationToken));

    [HttpPost("admissions/{id:guid}/complete-discharge")]
    [Authorize(Policy = Permissions.DischargeComplete)]
    public async Task<ActionResult<AdmissionDto>> CompleteDischarge(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CompleteDischargeCommand(id), cancellationToken));
}
