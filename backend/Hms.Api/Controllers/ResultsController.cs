using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using Hms.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
/// <summary>Lab and procedure request queues with result entry that survives encounter sign-off.</summary>
public sealed class ResultsController(IResultsService results, ICurrentUser user) : ControllerBase
{
    [HttpGet("tests")]
    [Authorize(Policy = Permissions.LabRequestsRead)]
    public async Task<IActionResult> Tests([FromQuery] ServiceRequestStatus? status, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await results.ListAsync(StockItemType.Test, status, search, cancellationToken));

    [HttpGet("procedures")]
    [Authorize(Policy = Permissions.ProceduresRequestsRead)]
    public async Task<IActionResult> Procedures([FromQuery] ServiceRequestStatus? status, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await results.ListAsync(StockItemType.Procedure, status, search, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await results.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Save(Guid id, SaveServiceResultRequest request, CancellationToken cancellationToken)
    {
        var kind = await results.GetAsync(id, cancellationToken);
        EnsureResultPermission(kind.Kind);
        return Ok(await results.SaveAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, SubmitServiceResultRequest request, CancellationToken cancellationToken)
    {
        var kind = await results.GetAsync(id, cancellationToken);
        EnsureResultPermission(kind.Kind);
        return Ok(await results.SubmitAsync(id, request, cancellationToken));
    }

    private void EnsureResultPermission(StockItemType kind)
    {
        var required = kind == StockItemType.Test ? Permissions.InvestigationsWrite : Permissions.SurgeriesWrite;
        if (user.Role is null || !PermissionCatalog.RoleHas(user.Role.Value, required))
            throw new AppForbiddenException($"Your role cannot record {kind} results.");
    }
}
