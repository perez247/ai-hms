using Hms.Application.DTOs;
using Hms.Application.Features.Patients;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
/// <summary>Reception registration/renewal and shared patient reads. Every action is a MediatR request.</summary>
public sealed class PatientsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.PatientsRead)]
    public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchPatientsQuery(search, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PatientsRead)]
    public async Task<ActionResult<PatientDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.PatientsCreate)]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await mediator.Send(new CreatePatientCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = patient.Id }, patient);
    }

    [HttpPost("{id:guid}/renew")]
    [Authorize(Policy = Permissions.PatientsRenew)]
    public async Task<IActionResult> Renew(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RenewPatientRegistrationCommand(id), cancellationToken));

    [HttpPost("{id:guid}/hmo-enrollments")]
    [Authorize(Policy = Permissions.PatientHmoManage)]
    public async Task<IActionResult> AddHmoEnrollment(Guid id, AddHmoEnrollmentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new AddPatientHmoEnrollmentCommand(id, request), cancellationToken));


    [HttpGet("number/{patientNumber}")]
    [Authorize(Policy = Permissions.PatientsProfileRead)]
    public async Task<ActionResult<PatientBasicProfileDto>> BasicByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientBasicProfileQuery(patientNumber), cancellationToken));

    [HttpGet("number/{patientNumber}/vitals")]
    [Authorize(Policy = Permissions.VitalsRead)]
    public async Task<IActionResult> VitalsByNumber(string patientNumber, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetPatientVitalsByNumberQuery(patientNumber, page, pageSize), cancellationToken));

    [HttpGet("number/{patientNumber}/encounters")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<IActionResult> EncountersByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientEncountersByNumberQuery(patientNumber), cancellationToken));

    [HttpGet("number/{patientNumber}/fitness")]
    [Authorize(Policy = Permissions.FitnessRead)]
    public async Task<IActionResult> FitnessByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientFitnessByNumberQuery(patientNumber), cancellationToken));

    [HttpGet("number/{patientNumber}/investigations")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<IActionResult> InvestigationsByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientInvestigationsByNumberQuery(patientNumber), cancellationToken));

    [HttpGet("number/{patientNumber}/surgeries")]
    [Authorize(Policy = Permissions.EncountersRead)]
    public async Task<IActionResult> SurgeriesByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientSurgeriesByNumberQuery(patientNumber), cancellationToken));

    [HttpGet("number/{patientNumber}/finance")]
    [Authorize(Policy = Permissions.PatientsProfileRead)]
    public async Task<IActionResult> FinanceByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientFinanceByNumberQuery(patientNumber), cancellationToken));

    [HttpGet("number/{patientNumber}/files")]
    [Authorize(Policy = Permissions.PatientsProfileRead)]
    public async Task<IActionResult> FilesByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientFilesByNumberQuery(patientNumber), cancellationToken));

    [HttpPost("number/{patientNumber}/files")]
    [Authorize(Policy = Permissions.PatientFilesUpload)]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<PatientFileDto>> UploadFileByNumber(string patientNumber, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest("File is empty.");
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(await mediator.Send(new UploadPatientFileByNumberCommand(patientNumber, file.FileName,
            file.ContentType ?? "application/octet-stream", stream.ToArray()), cancellationToken));
    }

    [HttpGet("number/{patientNumber}/files/{fileId:guid}")]
    [Authorize(Policy = Permissions.PatientsProfileRead)]
    public async Task<IActionResult> DownloadFileByNumber(string patientNumber, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await mediator.Send(new DownloadPatientFileByNumberQuery(patientNumber, fileId), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("number/{patientNumber}/hmo-enrollments")]
    [Authorize(Policy = Permissions.PatientsProfileRead)]
    public async Task<IActionResult> HmosByNumber(string patientNumber, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientHmosByNumberQuery(patientNumber), cancellationToken));

    [HttpPost("number/{patientNumber}/hmo-enrollments")]
    [Authorize(Policy = Permissions.PatientHmoManage)]
    public async Task<IActionResult> AddHmoEnrollmentByNumber(string patientNumber, AddHmoEnrollmentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new AddPatientHmoEnrollmentByNumberCommand(patientNumber, request), cancellationToken));

    [HttpPut("number/{patientNumber}/hmo-enrollments/{enrollmentId:guid}")]
    [Authorize(Policy = Permissions.PatientHmoManage)]
    public async Task<IActionResult> UpdateHmoEnrollmentByNumber(string patientNumber, Guid enrollmentId,
        AddHmoEnrollmentRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdatePatientHmoEnrollmentByNumberCommand(patientNumber, enrollmentId, request), cancellationToken));

    [HttpGet("{id:guid}/profile")]
    [Authorize(Policy = Permissions.PatientsProfileRead)]
    public async Task<ActionResult<PatientProfileDto>> Profile(Guid id, [FromQuery] int vitalsPage = 1,
        [FromQuery] int vitalsPageSize = 5, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetPatientProfileQuery(id, vitalsPage, vitalsPageSize), cancellationToken));

    [HttpGet("{id:guid}/vitals")]
    [Authorize(Policy = Permissions.VitalsRead)]
    public async Task<IActionResult> Vitals(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new GetPatientVitalsQuery(id, page, pageSize), cancellationToken));

    [HttpPost("{id:guid}/files")]
    [Authorize(Policy = Permissions.PatientFilesUpload)]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<PatientFileDto>> UploadFile(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest("File is empty.");
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(await mediator.Send(new UploadPatientFileCommand(id, file.FileName,
            file.ContentType ?? "application/octet-stream", stream.ToArray()), cancellationToken));
    }

    [HttpGet("{id:guid}/files/{fileId:guid}")]
    [Authorize(Policy = Permissions.PatientsProfileRead)]
    public async Task<IActionResult> DownloadFile(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        var file = await mediator.Send(new DownloadPatientFileQuery(id, fileId), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
