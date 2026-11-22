using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record SavePriorAuthorizationRequest(
    Guid HmoCompanyId,
    Guid HmoEnrollmentId,
    Guid? EncounterId,
    Guid? AdmissionId,
    string PaCode,
    DateTimeOffset? ExpiresAt,
    decimal? ApprovedAmount,
    string? Notes);

public sealed record PriorAuthorizationDto(
    Guid Id,
    Guid PatientId,
    Guid HmoCompanyId,
    string HmoName,
    Guid? EncounterId,
    Guid? AdmissionId,
    string PaCode,
    PriorAuthorizationStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ExpiresAt,
    decimal? ApprovedAmount,
    string? Notes);

public sealed record HmoLineCoverageRequest(
    Guid BillLineId,
    bool IsCovered,
    decimal HmoAmount,
    string? ReferralDestination);

public sealed record UpdateHmoCoverageRequest(IReadOnlyList<HmoLineCoverageRequest> Lines);

/// <summary>HMO Officer registration of a new HMO partner, optionally with a medication tariff in one step.</summary>
public sealed record RegisterHmoRequest(
    string Code,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? ContractTerms,
    CreateHmoTariffRequest? InitialTariff);

public sealed record HmoTariffItemRequest(
    string Code,
    string Description,
    TariffCategory Category,
    string? Unit,
    decimal UnitPrice,
    decimal CoveragePercent,
    bool RequiresPriorAuthorization);

public sealed record CreateHmoTariffRequest(
    string Name,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyList<HmoTariffItemRequest> Items);

public sealed record HmoTariffItemDto(
    Guid Id,
    string Code,
    string Description,
    TariffCategory Category,
    string? Unit,
    decimal UnitPrice,
    decimal CoveragePercent,
    bool RequiresPriorAuthorization,
    bool IsActive);

public sealed record HmoTariffDto(
    Guid Id,
    Guid HmoCompanyId,
    string Name,
    int Version,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    TariffStatus Status,
    IReadOnlyList<HmoTariffItemDto> Items);

public sealed record HmoDirectoryEntryDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    IReadOnlyList<HmoTariffDto> Tariffs);

/// <summary>One medication price option (private market price or an HMO negotiated tariff).</summary>
public sealed record MedicationPriceOptionDto(
    string PayerType,
    Guid? HmoCompanyId,
    string Label,
    decimal TotalPrice,
    decimal HmoCoveredAmount,
    decimal PatientAmount,
    bool Undercovered,
    IReadOnlyList<string> Warnings);

public sealed record PrescriptionPriceOptionsDto(
    Guid EncounterId,
    decimal MarketTotal,
    IReadOnlyList<MedicationPriceOptionDto> Options);
