using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record CreateFeeRequest(FeeCode Code, decimal Amount, string Currency, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record FeeDto(Guid Id, FeeCode Code, decimal Amount, string Currency, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive);

public sealed record CreateHmoCompanyRequest(
    string Code,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? ContractTerms,
    string? MetadataJson);

public sealed record CreateHmoContractRequest(DateOnly StartDate, DateOnly EndDate, string? Terms, string? DocumentRef);
public sealed record HmoCompanyDto(Guid Id, string Code, string Name, string? ContactPerson, string? Email, string? Phone, string? Address, bool IsActive);
public sealed record HmoContractDto(Guid Id, DateOnly StartDate, DateOnly EndDate, HmoContractStatus Status);

public sealed record TariffImportResultDto(Guid TariffId, int Version, int ItemsImported, IReadOnlyList<string> Errors);
public sealed record UpdateSettingRequest(string ValueJson, string? Description);
public sealed record ClinicSettingDto(string Key, string ValueJson, string? Description, DateTimeOffset UpdatedAt);
