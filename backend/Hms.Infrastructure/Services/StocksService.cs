using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Stock items (physical products with quantity) and their independent per-payer prices.</summary>
public sealed class StocksService(HmsDbContext db, ICurrentUser user, IAuditWriter audit) : IStocksService
{
    public async Task<IReadOnlyList<PayerOptionDto>> GetPayersAsync(CancellationToken cancellationToken = default)
    {
        var hmos = await db.HmoCompanies.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new PayerOptionDto(x.Id, x.Name, false)).ToListAsync(cancellationToken);
        return [new PayerOptionDto(Payers.PrivateId, Payers.PrivateName, true), .. hmos];
    }

    public async Task<IReadOnlyList<StockItemDto>> ListAsync(StockItemType? type, string? search, Guid? payerId,
        bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var term = (search ?? string.Empty).Trim().ToLowerInvariant();
        var query = db.StockItems.AsNoTracking()
            .Include(x => x.Prices).ThenInclude(x => x.HmoCompany)
            .Where(x => includeInactive || x.IsActive);
        if (type is not null) query = query.Where(x => x.ItemType == type.Value);
        if (term.Length > 0) query = query.Where(x => x.Name.ToLower().Contains(term) || x.Code.ToLower().Contains(term));
        if (payerId is not null) query = query.Where(x => x.Prices.Any(p => p.PayerId == payerId.Value));
        var items = await query.OrderBy(x => x.Name).Take(500).ToListAsync(cancellationToken);
        return items.Select(item => Map(item, payerId)).ToList();
    }

    public async Task<StockItemDto> CreateAsync(CreateStockItemRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new AppConflictException("Code and name are required.");
        var code = request.Code.Trim();
        if (await db.StockItems.AnyAsync(x => x.Code == code, cancellationToken))
            throw new AppConflictException("A stock item with this code already exists.");
        if (request.Quantity < 0) throw new AppConflictException("Quantity cannot be negative.");
        var item = new StockItem
        {
            Code = code, Name = request.Name.Trim(), ItemType = request.ItemType,
            Category = request.Category, GenericName = request.GenericName, Form = request.Form,
            Strength = request.Strength, Unit = string.IsNullOrWhiteSpace(request.Unit) ? "unit" : request.Unit,
            TariffCode = request.TariffCode, Quantity = request.Quantity, CreatedBy = user.RequireId()
        };
        db.StockItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("stock.created", nameof(StockItem), item.Id.ToString(),
            after: new { item.Code, item.ItemType, item.Quantity }, cancellationToken: cancellationToken);
        return Map(item, null);
    }

    public async Task<StockItemDto> UpdateAsync(Guid id, UpdateStockItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await db.StockItems.Include(x => x.Prices).ThenInclude(x => x.HmoCompany)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Stock item not found.");
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new AppConflictException("Code and name are required.");
        var code = request.Code.Trim();
        if (await db.StockItems.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            throw new AppConflictException("A stock item with this code already exists.");
        if (request.Quantity < 0) throw new AppConflictException("Quantity cannot be negative.");
        item.Code = code;
        item.Name = request.Name.Trim();
        item.ItemType = request.ItemType;
        item.Quantity = request.Quantity;
        item.Category = request.Category;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("stock.updated", nameof(StockItem), id.ToString(),
            after: new { item.Code, item.Name, item.ItemType, item.Quantity }, cancellationToken: cancellationToken);
        return Map(item, null);
    }

    public async Task<StockItemDto> SetPriceAsync(Guid id, SetItemPriceRequest request, CancellationToken cancellationToken = default)
    {
        var item = await db.StockItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Stock item not found.");
        if (request.UnitPrice < 0) throw new AppConflictException("Price cannot be negative.");
        var isPrivate = Payers.IsPrivate(request.PayerId);
        Guid? hmoCompanyId = null;
        if (!isPrivate)
        {
            hmoCompanyId = await db.HmoCompanies.Where(x => x.Id == request.PayerId && x.IsActive)
                .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken)
                ?? throw new AppConflictException("Unknown or inactive HMO payer.");
        }
        var existing = await db.ItemPrices.SingleOrDefaultAsync(x => x.StockItemId == id && x.PayerId == request.PayerId, cancellationToken);
        if (existing is null)
        {
            db.ItemPrices.Add(new ItemPrice
            {
                StockItemId = id, PayerId = request.PayerId,
                FundingSource = isPrivate ? FundingSource.Private : FundingSource.Hmo,
                HmoCompanyId = hmoCompanyId, UnitPrice = request.UnitPrice,
                CoveragePercent = request.CoveragePercent, RequiresPriorAuthorization = request.RequiresPriorAuthorization,
                EffectiveFrom = request.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EffectiveTo = request.EffectiveTo, IsActive = true
            });
        }
        else
        {
            existing.UnitPrice = request.UnitPrice;
            existing.CoveragePercent = request.CoveragePercent;
            existing.RequiresPriorAuthorization = request.RequiresPriorAuthorization;
            existing.EffectiveFrom = request.EffectiveFrom ?? existing.EffectiveFrom;
            existing.EffectiveTo = request.EffectiveTo;
            existing.IsActive = true;
        }
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("stock.price-set", nameof(StockItem), id.ToString(),
            after: new { request.PayerId, request.UnitPrice }, cancellationToken: cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<StockItemDto> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        var item = await db.StockItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Stock item not found.");
        var quantity = item.Quantity + request.QuantityDelta;
        if (quantity < 0) throw new AppConflictException("Stock quantity cannot become negative.");
        item.Quantity = quantity;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        db.StockMovements.Add(new StockMovement
        {
            StockItemId = id,
            MovementType = request.QuantityDelta >= 0 ? StockMovementType.Receipt : StockMovementType.Adjustment,
            QuantityDelta = request.QuantityDelta, Notes = request.Notes, CreatedBy = user.RequireId()
        });
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("stock.adjusted", nameof(StockItem), id.ToString(), after: new { item.Quantity }, cancellationToken: cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<StockItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await db.StockItems.Include(x => x.Prices).ThenInclude(x => x.HmoCompany)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Stock item not found.");
        return Map(item, null);
    }

    private static StockItemDto Map(StockItem item, Guid? payerId)
    {
        var prices = item.Prices.AsEnumerable();
        if (payerId is not null) prices = prices.Where(x => x.PayerId == payerId.Value);
        return new StockItemDto(item.Id, item.Code, item.Name, item.ItemType, item.Category,
            item.GenericName, item.Form, item.Strength, item.Unit, item.TariffCode, item.IsActive, item.Quantity,
            prices.OrderBy(x => x.FundingSource).ThenBy(x => x.EffectiveFrom).Select(x => new ItemPriceDto(
                x.Id, x.PayerId, x.FundingSource == FundingSource.Private ? Payers.PrivateName : x.HmoCompany?.Name ?? "HMO",
                x.FundingSource, x.HmoCompanyId, x.HmoCompany?.Name, x.UnitPrice,
                x.CoveragePercent, x.RequiresPriorAuthorization, x.EffectiveFrom, x.EffectiveTo, x.IsActive)).ToList());
    }
}
