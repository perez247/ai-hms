using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IAuditQueryService
{
    Task<PagedResult<AuditLogDto>> SearchAsync(string? entityType, Guid? billId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken cancellationToken = default);
}
