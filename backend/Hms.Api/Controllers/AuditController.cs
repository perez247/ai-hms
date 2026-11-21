using Hms.Application.Features.Audit;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
/// <summary>Admin-only audit-log search.</summary>
public sealed class AuditController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.AuditRead)]
    public async Task<IActionResult> Search([FromQuery] string? entityType, [FromQuery] Guid? billId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchAuditLogsQuery(entityType, billId, from, to, page, pageSize), cancellationToken));
}
