using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using Hms.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Controllers;

[ApiController]
[Route("api/stocks")]
[Authorize]
/// <summary>Stock item and price management. Management rights are Pharmacy/Admin only.</summary>
public sealed class StocksController(IStocksService stocks, IStockImportService imports) : ControllerBase
{
    [HttpGet("payers")]
    [Authorize(Policy = Permissions.StockRead)]
    public async Task<IActionResult> Payers(CancellationToken cancellationToken) =>
        Ok(await stocks.GetPayersAsync(cancellationToken));

    [HttpGet]
    [Authorize(Policy = Permissions.StockRead)]
    public async Task<IActionResult> List([FromQuery] StockItemType? type, [FromQuery] string? search,
        [FromQuery] Guid? payerId, [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok(await stocks.ListAsync(type, search, payerId, includeInactive, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.StockRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await stocks.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.StockManage)]
    public async Task<IActionResult> Create(CreateStockItemRequest request, CancellationToken cancellationToken) =>
        Ok(await stocks.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.StockManage)]
    public async Task<IActionResult> Update(Guid id, UpdateStockItemRequest request, CancellationToken cancellationToken) =>
        Ok(await stocks.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/prices")]
    [Authorize(Policy = Permissions.StockManage)]
    public async Task<IActionResult> SetPrice(Guid id, SetItemPriceRequest request, CancellationToken cancellationToken) =>
        Ok(await stocks.SetPriceAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/adjust")]
    [Authorize(Policy = Permissions.PharmacyStockManage)]
    public async Task<IActionResult> Adjust(Guid id, AdjustStockRequest request, CancellationToken cancellationToken) =>
        Ok(await stocks.AdjustStockAsync(id, request, cancellationToken));

    [HttpGet("template")]
    [Authorize(Policy = Permissions.StockRead)]
    public IActionResult Template([FromQuery] string kind = "items", [FromQuery] string format = "csv")
    {
        if (!kind.Equals("items", StringComparison.OrdinalIgnoreCase) && !kind.Equals("prices", StringComparison.OrdinalIgnoreCase))
            return BadRequest("kind must be 'items' or 'prices'.");
        if (!format.Equals("csv", StringComparison.OrdinalIgnoreCase) && !format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("format must be 'csv' or 'xlsx'.");
        var (content, contentType, fileName) = imports.BuildTemplate(kind, format);
        return File(content, contentType, fileName);
    }

    [HttpPost("import/items")]
    [Authorize(Policy = Permissions.StockManage)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportItems(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("Upload file is empty.");
        return Ok(await imports.ImportItemsAsync(file.OpenReadStream(), file.FileName, cancellationToken));
    }

    [HttpPost("import/prices")]
    [Authorize(Policy = Permissions.StockManage)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportPrices(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("Upload file is empty.");
        return Ok(await imports.ImportPricesAsync(file.OpenReadStream(), file.FileName, cancellationToken));
    }
}
