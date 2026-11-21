using Hms.Application.DTOs;
using Hms.Application.Features.Hr;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/hr")]
[Authorize]
/// <summary>HR user lifecycle and CSV export; no clinical access.</summary>
public sealed class HrController(IMediator mediator) : ControllerBase
{
    [HttpGet("users")]
    [Authorize(Policy = Permissions.HrUsersRead)]
    public async Task<IActionResult> Search([FromQuery] bool? active, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await mediator.Send(new SearchUsersQuery(active, page, pageSize), cancellationToken));

    [HttpPost("users")]
    [Authorize(Policy = Permissions.HrUsersManage)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateUserCommand(request), cancellationToken));

    [HttpPut("users/{id:guid}/payroll")]
    [Authorize(Policy = Permissions.HrPayrollManage)]
    public async Task<ActionResult<UserDto>> UpdatePayroll(Guid id, UpdatePayrollRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdatePayrollCommand(id, request), cancellationToken));

    [HttpPost("users/{id:guid}/disable")]
    [Authorize(Policy = Permissions.HrUsersManage)]
    public async Task<ActionResult<UserDto>> Disable(Guid id, DisableUserRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DisableUserCommand(id, request), cancellationToken));

    [HttpGet("users/export")]
    [Authorize(Policy = Permissions.HrExport)]
    public async Task<IActionResult> Export([FromQuery] bool? active, CancellationToken cancellationToken) =>
        File(await mediator.Send(new ExportUsersQuery(active), cancellationToken), "text/csv", $"users-{DateTime.UtcNow:yyyyMMdd}.csv");
}
