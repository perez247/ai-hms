using Hms.Application.DTOs;
using Hms.Application.Features.Admin;
using Hms.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
/// <summary>Admin fee/HMO/tariff/settings configuration.</summary>
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    [HttpGet("fees")]
    [Authorize(Policy = Permissions.AdminFeesManage)]
    public async Task<IActionResult> GetFees(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetFeesQuery(), cancellationToken));

    [HttpPost("fees")]
    [Authorize(Policy = Permissions.AdminFeesManage)]
    public async Task<ActionResult<FeeDto>> CreateFee(CreateFeeRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateFeeCommand(request), cancellationToken));

    [HttpGet("hmos")]
    [Authorize(Policy = Permissions.AdminHmosManage)]
    public async Task<IActionResult> GetHmos(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetHmosQuery(), cancellationToken));

    [HttpPost("hmos")]
    [Authorize(Policy = Permissions.AdminHmosManage)]
    public async Task<ActionResult<HmoCompanyDto>> CreateHmo(CreateHmoCompanyRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateHmoCommand(request), cancellationToken));

    [HttpPost("hmos/{id:guid}/contracts")]
    [Authorize(Policy = Permissions.AdminHmosManage)]
    public async Task<ActionResult<HmoContractDto>> CreateContract(Guid id, CreateHmoContractRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateHmoContractCommand(id, request), cancellationToken));

    [HttpPost("hmos/{id:guid}/tariffs")]
    [Authorize(Policy = Permissions.AdminTariffsManage)]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<TariffImportResultDto>> ImportTariff(Guid id, IFormFile file,
        [FromForm] DateOnly effectiveFrom, [FromForm] DateOnly? effectiveTo, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest("Tariff file is empty.");
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(await mediator.Send(new ImportTariffCommand(id, file.FileName, stream.ToArray(), effectiveFrom, effectiveTo), cancellationToken));
    }

    [HttpGet("hmos/{id:guid}/tariffs/{tariffId:guid}/export")]
    [Authorize(Policy = Permissions.AdminTariffsManage)]
    public async Task<IActionResult> ExportTariff(Guid id, Guid tariffId, [FromQuery] string? category,
        [FromQuery] string? code, CancellationToken cancellationToken) =>
        File(await mediator.Send(new ExportTariffQuery(id, tariffId, category, code), cancellationToken), "text/csv", $"tariff-{tariffId}.csv");

    [HttpGet("settings")]
    [Authorize(Policy = Permissions.AdminSettingsManage)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetClinicSettingsQuery(), cancellationToken));

    [HttpPut("settings/{key}")]
    [Authorize(Policy = Permissions.AdminSettingsManage)]
    public async Task<ActionResult<ClinicSettingDto>> UpdateSetting(string key, UpdateSettingRequest request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateClinicSettingCommand(key, request), cancellationToken));
}
