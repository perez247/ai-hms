using Hms.Application.DTOs;
using Hms.Application.Features.Pharmacy;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/pharmacy")]
[Authorize]
/// <summary>Pharmacy queue, stock issue and tariff confirmation. Pharmacy never approves payment.</summary>
public sealed class PharmacyController(IMediator mediator) : ControllerBase
{
    [HttpGet("queue")]
    [Authorize(Policy = Permissions.PharmacyRead)]
    public async Task<IActionResult> Queue(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPharmacyQueueQuery(), cancellationToken));

    [HttpGet("encounters/{encounterId:guid}/prescription")]
    [Authorize(Policy = Permissions.PharmacyRead)]
    public async Task<ActionResult<PharmacyPrescriptionDto>> GetPrescription(Guid encounterId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPharmacyPrescriptionQuery(encounterId), cancellationToken));

    [HttpGet("encounters/{encounterId:guid}/price-options")]
    [Authorize(Policy = Permissions.PharmacyRead)]
    public async Task<ActionResult<PrescriptionPriceOptionsDto>> GetPriceOptions(Guid encounterId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetPrescriptionPriceOptionsQuery(encounterId), cancellationToken));

    [HttpPost("dispenses")]
    [Authorize(Policy = Permissions.PharmacyDispense)]
    public async Task<ActionResult<DispenseResultDto>> Dispense(DispenseRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DispenseCommand(request), cancellationToken));

    [HttpPost("bills/{billId:guid}/confirm-tariff")]
    [Authorize(Policy = Permissions.PharmacyTariffConfirm)]
    public async Task<IActionResult> ConfirmTariff(Guid billId, ConfirmTariffRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new ConfirmTariffCommand(billId, request), cancellationToken);
        return NoContent();
    }
}
