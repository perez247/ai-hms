using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Finance account-book reads and expense posting.</summary>
public sealed partial class BillingService
{

    public async Task<PagedResult<AccountBookEntryDto>> GetAccountBookAsync(DateOnly? from, DateOnly? to, AccountEntryType? type, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.AccountBookEntries.AsNoTracking().AsQueryable();
        if (from is not null)
        {
            var start = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(x => x.OccurredAt >= start);
        }
        if (to is not null)
        {
            var end = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(x => x.OccurredAt < end);
        }
        if (type is not null) query = query.Where(x => x.EntryType == type);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AccountBookEntryDto(x.Id, x.EntryNumber, x.EntryType, x.EntryType == AccountEntryType.Income ? "Credit" : "Debit", x.Category, x.Amount, x.Currency, x.OccurredAt, x.Description, x.Reference, x.BillId, x.PaymentId))
            .ToListAsync(cancellationToken);
        return new PagedResult<AccountBookEntryDto>(items, page, pageSize, total);
    }

    public async Task<AccountBookEntryDto> AddExpenseAsync(AccountBookEntryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EntryType != AccountEntryType.Expense) throw new AppConflictException("This endpoint records expenses only.");
        if (request.Amount <= 0) throw new AppConflictException("Expense must be greater than zero.");
        var entry = new AccountBookEntry
        {
            EntryNumber = await numbers.NextAsync("account_entry_seq", "ACC", cancellationToken),
            EntryType = AccountEntryType.Expense,
            Category = request.Category,
            Amount = request.Amount,
            Description = request.Description,
            Reference = request.Reference,
            OccurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow,
            CreatedBy = user.RequireId()
        };
        db.AccountBookEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("accountbook.expense", nameof(AccountBookEntry), entry.Id.ToString(), after: new { entry.Amount, entry.Category }, cancellationToken: cancellationToken);
        return new AccountBookEntryDto(entry.Id, entry.EntryNumber, entry.EntryType, "Debit", entry.Category, entry.Amount, entry.Currency, entry.OccurredAt, entry.Description, entry.Reference, null, null);
    }
}
