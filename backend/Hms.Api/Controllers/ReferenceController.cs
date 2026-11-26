using Hms.Application.Features.Reference;
using Hms.Application.Security;
using Hms.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/reference")]
[Authorize(Policy = Permissions.PatientsRead)]
/// <summary>Lookup data for form selects; read policy is shared with patient access.</summary>
public sealed class ReferenceController(IMediator mediator) : ControllerBase
{
    [HttpGet("hmos")]
    public async Task<IActionResult> Hmos(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetHmoOptionsQuery(), cancellationToken));

    [HttpGet("doctors")]
    public async Task<IActionResult> Doctors(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetDoctorOptionsQuery(), cancellationToken));

    [HttpGet("patients/{patientId:guid}/hmo-enrollments")]
    public async Task<IActionResult> Enrollments(Guid patientId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPatientHmoEnrollmentsQuery(patientId), cancellationToken));

    [HttpGet("medications")]
    public async Task<IActionResult> Medications(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetMedicationOptionsQuery(), cancellationToken));

    [HttpGet("investigations")]
    public async Task<IActionResult> Investigations(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetInvestigationOptionsQuery(), cancellationToken));

    [HttpGet("procedures")]
    public async Task<IActionResult> Procedures(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetProcedureOptionsQuery(), cancellationToken));

    /// <summary>Read-only orderable-item lookup for the doctor's encounter form (no stock management rights required).</summary>
    [HttpGet("stock-items")]
    public async Task<IActionResult> StockItems([FromQuery] StockItemType? type, [FromQuery] Guid? payerId,
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStockItemsQuery(type, payerId, search), cancellationToken));
}
