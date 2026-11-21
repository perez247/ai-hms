using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IHmoService
{
    Task<PriorAuthorizationDto> SavePriorAuthorizationAsync(SavePriorAuthorizationRequest request, CancellationToken cancellationToken = default);
    Task<BillDto> UpdateCoverageAsync(Guid billId, UpdateHmoCoverageRequest request, CancellationToken cancellationToken = default);
    Task<Bill> ApproveAsync(Guid billId, CancellationToken cancellationToken = default);
    Task<PagedResult<PriorAuthorizationDto>> SearchAuthorizationsAsync(Guid? patientId, int page, int pageSize, CancellationToken cancellationToken = default);
}
