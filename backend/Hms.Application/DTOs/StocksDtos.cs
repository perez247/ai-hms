using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record PayerOptionDto(Guid Id, string Name, bool IsPrivate);

public sealed record ItemPriceDto(
    Guid Id, Guid PayerId, string PayerName, FundingSource FundingSource, Guid? HmoCompanyId, string? HmoName,
    decimal UnitPrice, decimal CoveragePercent, bool RequiresPriorAuthorization,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive);

public sealed record StockItemDto(
    Guid Id, string Code, string Name, StockItemType ItemType, string? Category,
    string? GenericName, string? Form, string? Strength, string Unit, string? TariffCode,
    bool IsActive, decimal Quantity,
    IReadOnlyList<ItemPriceDto> Prices);

public sealed record CreateStockItemRequest(
    string Code, string Name, StockItemType ItemType, string? Category,
    string? GenericName, string? Form, string? Strength, string Unit, string? TariffCode,
    decimal Quantity = 0);

public sealed record UpdateStockItemRequest(
    string Code, string Name, StockItemType ItemType, decimal Quantity, bool IsActive, string? Category = null);

public sealed record SetItemPriceRequest(
    Guid PayerId, decimal UnitPrice, int CoveragePercent = 100,
    bool RequiresPriorAuthorization = false, DateOnly? EffectiveFrom = null, DateOnly? EffectiveTo = null);

public sealed record AdjustStockRequest(decimal QuantityDelta, string? Notes);

public sealed record ImportErrorDto(int Row, string Column, string Message);

public sealed record ImportResultDto(int TotalRows, int Imported, IReadOnlyList<ImportErrorDto> Errors);
