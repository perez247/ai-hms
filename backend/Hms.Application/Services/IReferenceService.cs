using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IReferenceService
{    Task<IReadOnlyList<HmoOptionDto>> GetHmosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorOptionDto>> GetDoctorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HmoEnrollmentOptionDto>> GetPatientHmoEnrollmentsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceOptionDto>> GetMedicationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceOptionDto>> GetInvestigationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceOptionDto>> GetProceduresAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockItemDto>> GetStockItemsAsync(StockItemType? type, Guid? payerId, string? search, CancellationToken cancellationToken = default);
}
