using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IAdminService
{
    Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default);
    Task<FeeDto> CreateFeeAsync(CreateFeeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HmoCompanyDto>> GetHmosAsync(CancellationToken cancellationToken = default);
    Task<HmoCompanyDto> CreateHmoAsync(CreateHmoCompanyRequest request, CancellationToken cancellationToken = default);
    Task<HmoContractDto> CreateContractAsync(Guid hmoCompanyId, CreateHmoContractRequest request, CancellationToken cancellationToken = default);
    Task<TariffImportResultDto> ImportTariffAsync(Guid hmoCompanyId, string fileName, Stream csv, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default);
    Task<byte[]> ExportTariffAsync(Guid hmoCompanyId, Guid tariffId, string? category, string? code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicSettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<ClinicSettingDto> UpdateSettingAsync(string key, UpdateSettingRequest request, CancellationToken cancellationToken = default);
}
