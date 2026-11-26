using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Finance-initiated bill line edits: add service items and remove existing ones.</summary>
public sealed partial class BillingService
{
    public async Task<BillLine> AddFinanceLineAsync(Guid billId, AddBillLineRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0) throw new AppConflictException("Quantity must be greater than zero.");
        var bill = await LoadBillAsync(billId, cancellationToken);
        if (bill.Status is BillStatus.Approved or BillStatus.Paid or BillStatus.HmoApproved or BillStatus.Void)
            throw new AppConflictException("This bill can no longer be edited.");

        var item = await db.StockItems.SingleOrDefaultAsync(x => x.Id == request.StockItemId && x.IsActive, cancellationToken)
            ?? throw new AppNotFoundException("Stock item not found.");
        var payerId = bill.PayerType == PayerType.Hmo && bill.HmoCompanyId is not null ? bill.HmoCompanyId.Value : Payers.PrivateId;
        var price = await db.ItemPrices.AsNoTracking()
            .Where(x => x.StockItemId == item.Id && x.PayerId == payerId && x.IsActive)
            .OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppConflictException($"No price is configured for {item.Name} and this payer.");

        var category = item.ItemType switch
        {
            StockItemType.Medication => BillLineCategory.Medication,
            StockItemType.Test => BillLineCategory.Investigation,
            StockItemType.Procedure => BillLineCategory.Surgery,
            _ => BillLineCategory.Consultation
        };
        var unitPrice = price.UnitPrice;
        if (bill.PayerType == PayerType.Hmo && item.TariffCode is not null)
        {
            var tariff = await tariffs.ResolveOneAsync(bill.HmoCompanyId!.Value, item.TariffCode, cancellationToken);
            if (tariff is not null) unitPrice = tariff.UnitPrice;
        }

        var line = new BillLine
        {
            BillId = bill.Id,
            Category = category,
            Description = string.IsNullOrWhiteSpace(request.Description) ? item.Name : request.Description.Trim(),
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            DiscountAmount = 0m,
            IsHmoCovered = bill.PayerType == PayerType.Hmo,
            SourceType = ChargeSourceType.Encounter,
            SourceId = bill.Id,
            SourceKey = $"finance-line:{Guid.NewGuid():N}",
            ChargeDate = DateTimeOffset.UtcNow,
            CreatedBy = user.RequireId()
        };
        var coverage = bill.PayerType == PayerType.Hmo ? price.CoveragePercent : 0m;
        BillingCalculator.SetLineAmounts(line, coverage, bill.PayerType == PayerType.Hmo);
        bill.Lines.Add(line);
        db.BillLines.Add(line);
        BillingCalculator.Recalculate(bill);
        bill.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.finance-line-added", nameof(BillLine), line.Id.ToString(), bill.PatientId,
            bill.EncounterId, bill.Id, after: new { line.Category, line.Description, line.NetAmount }, cancellationToken: cancellationToken);
        return line;
    }

    public async Task<Bill> RemoveFinanceLineAsync(Guid billId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var bill = await LoadBillAsync(billId, cancellationToken);
        if (bill.Status is BillStatus.Approved or BillStatus.Paid or BillStatus.HmoApproved or BillStatus.Void)
            throw new AppConflictException("This bill can no longer be edited.");
        var line = bill.Lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new AppNotFoundException("Bill line not found.");
        bill.Lines.Remove(line);
        db.BillLines.Remove(line);
        BillingCalculator.Recalculate(bill);
        bill.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.finance-line-removed", nameof(BillLine), line.Id.ToString(), bill.PatientId,
            bill.EncounterId, bill.Id, after: new { line.Description, line.NetAmount }, cancellationToken: cancellationToken);
        return bill;
    }
}
