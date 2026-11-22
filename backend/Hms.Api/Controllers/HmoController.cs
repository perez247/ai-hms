using Hms.Application.DTOs;
using Hms.Application.Features.Hmo;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/hmo")]
[Authorize]
/// <summary>HMO Office PA, coverage and HMO-only approval. Finance approval is separate.</summary>
public sealed class HmoController(IMediator mediator) : ControllerBase
{
    [HttpGet("companies")]
    [Authorize(Policy = Permissions.HmoRead)]
    public async Task<IActionResult> GetDirectory(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetHmoDirectoryQuery(), cancellationToken));

    [HttpPost("companies")]
    [Authorize(Policy = Permissions.HmoCompanyManage)]
    public async Task<ActionResult<HmoDirectoryEntryDto>> Register(RegisterHmoRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RegisterHmoCommand(request), cancellationToken));

    [HttpPost("companies/{id:guid}/tariffs")]
    [Authorize(Policy = Permissions.HmoTariffManage)]
    public async Task<ActionResult<HmoTariffDto>> SaveTariff(Guid id, CreateHmoTariffRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveHmoTariffCommand(id, request), cancellationToken));

    [HttpGet("prior-authorizations")]
    [Authorize(Policy = Permissions.HmoRead)]
    public async Task<IActionResult> Search([FromQuery] Guid? patientId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchPriorAuthorizationsQuery(patientId, page, pageSize), cancellationToken));

    [HttpPost("prior-authorizations")]
    [Authorize(Policy = Permissions.HmoPriorAuthManage)]
    public async Task<ActionResult<PriorAuthorizationDto>> SavePriorAuthorization(SavePriorAuthorizationRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SavePriorAuthorizationCommand(request), cancellationToken));

    [HttpPost("bills/{billId:guid}/coverage")]
    [Authorize(Policy = Permissions.HmoCoverageManage)]
    public async Task<ActionResult<BillDto>> UpdateCoverage(Guid billId, UpdateHmoCoverageRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateHmoCoverageCommand(billId, request), cancellationToken));

    [HttpPost("bills/{billId:guid}/approve")]
    [Authorize(Policy = Permissions.HmoApprove)]
    public async Task<ActionResult<BillDto>> Approve(Guid billId, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ApproveHmoBillCommand(billId), cancellationToken));
}
