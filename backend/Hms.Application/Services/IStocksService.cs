using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IStocksService
{
    Task<IReadOnlyList<PayerOptionDto>> GetPayersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItemDto>> ListAsync(StockItemType? type, string? search, Guid? payerId,
        bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<StockItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockItemDto> CreateAsync(CreateStockItemRequest request, CancellationToken cancellationToken = default);
    Task<StockItemDto> UpdateAsync(Guid id, UpdateStockItemRequest request, CancellationToken cancellationToken = default);
    Task<StockItemDto> SetPriceAsync(Guid id, SetItemPriceRequest request, CancellationToken cancellationToken = default);
    Task<StockItemDto> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken cancellationToken = default);
}
