using Hms.Application.Features.Staff;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

/// <summary>Own profile for every authenticated user; HR/Admin may read any staff profile.</summary>
[ApiController]
[Route("api/staff")]
[Authorize]
public sealed class StaffController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStaffProfileQuery(null), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.HrUsersRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStaffProfileQuery(id), cancellationToken));
}
