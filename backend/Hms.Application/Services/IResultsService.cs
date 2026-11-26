using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IResultsService
{
    Task<IReadOnlyList<ServiceRequestDto>> ListAsync(StockItemType kind, ServiceRequestStatus? status,
        string? search, CancellationToken cancellationToken = default);

    Task<ServiceRequestDto> GetAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto> SaveAsync(Guid requestId, SaveServiceResultRequest request, CancellationToken cancellationToken = default);
    Task<ServiceRequestDto> SubmitAsync(Guid requestId, SubmitServiceResultRequest request, CancellationToken cancellationToken = default);
}
