using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IPharmacyService
{
    Task<IReadOnlyList<PharmacyQueueItemDto>> GetQueueAsync(CancellationToken cancellationToken = default);
    Task<PharmacyPrescriptionDto> GetPrescriptionAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task<PrescriptionPriceOptionsDto> GetPriceOptionsAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task<DispenseResultDto> DispenseAsync(DispenseRequest request, CancellationToken cancellationToken = default);
    Task ConfirmTariffAsync(Guid billId, ConfirmTariffRequest request, CancellationToken cancellationToken = default);
}
