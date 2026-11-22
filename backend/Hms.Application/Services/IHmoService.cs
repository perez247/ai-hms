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

    // HMO Officer self-service registration and tariff administration.
    Task<IReadOnlyList<HmoDirectoryEntryDto>> GetDirectoryAsync(CancellationToken cancellationToken = default);
    Task<HmoDirectoryEntryDto> RegisterAsync(RegisterHmoRequest request, CancellationToken cancellationToken = default);
    Task<HmoTariffDto> SaveTariffAsync(Guid hmoCompanyId, CreateHmoTariffRequest request, CancellationToken cancellationToken = default);
}
