using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Read-only audit-log queries for the Admin audit screen.</summary>
public sealed class AuditQueryService(HmsDbContext db) : IAuditQueryService
{
    public async Task<PagedResult<AuditLogDto>> SearchAsync(string? entityType, Guid? billId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
        if (billId is not null) query = query.Where(x => x.BillId == billId);
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
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogDto(x.Id, x.OccurredAt, x.ActorUserId, x.ActorRole, x.Action, x.EntityType,
                x.EntityId, x.PatientId, x.EncounterId, x.BillId, x.BeforeJson, x.AfterJson))
            .ToListAsync(cancellationToken);
        return new PagedResult<AuditLogDto>(rows, page, pageSize, total);
    }
}
