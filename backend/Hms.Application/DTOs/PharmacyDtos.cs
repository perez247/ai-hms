namespace Hms.Application.DTOs;

public sealed record PharmacyQueueItemDto(Guid EncounterId, string EncounterNumber, string PatientName, bool IsInpatient, string PrescriptionStatus, DateTimeOffset CreatedAt);

public sealed record PharmacyPrescriptionItemDto(
    Guid Id,
    Guid MedicationId,
    string MedicationName,
    string? Strength,
    string? Form,
    string Unit,
    decimal StockOnHand,
    decimal QuantityPrescribed,
    decimal QuantityDispensed,
    decimal UnitPrice,
    string? TariffCode,
    decimal? HmoUnitPrice,
    decimal? HmoCoveragePercent,
    bool IsInpatient);

public sealed record PharmacyPrescriptionDto(
    Guid Id,
    Guid EncounterId,
    string EncounterNumber,
    string PatientName,
    bool IsInpatient,
    string Status,
    string? Notes,
    IReadOnlyList<PharmacyPrescriptionItemDto> Items);

public sealed record DispenseItemRequest(Guid PrescriptionItemId, decimal Quantity);

public sealed record DispenseRequest(Guid PrescriptionId, IReadOnlyList<DispenseItemRequest> Items);

public sealed record DispenseResultDto(
    Guid DispenseId,
    Guid BillId,
    string BillStatus,
    bool CommittedToFinance,
    decimal PatientAmount,
    decimal HmoAmount,
    IReadOnlyList<string> Warnings);

public sealed record ConfirmTariffRequest(IReadOnlyList<Guid> BillLineIds);
