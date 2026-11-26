using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record ServiceRequestDto(
    Guid Id, string RequestNumber, Guid EncounterId, string EncounterNumber,
    Guid PatientId, string PatientNumber, string PatientName,
    Guid StockItemId, string ItemName, StockItemType Kind,
    ServiceRequestStatus Status, DateTimeOffset OrderedAt, string DoctorName,
    FundingSource FundingSource, string? HmoName, string? Priority, string? ClinicalNote,
    Guid? ResultId, ServiceResultStatus? ResultStatus, string? ResultText, string? Findings,
    bool? AbnormalFlag, int Version);

public sealed record SaveServiceResultRequest(
    string? ResultText, string? Findings, string? StructuredJson, bool? AbnormalFlag, string? Reason);

public sealed record SubmitServiceResultRequest(
    string ResultText, string? Findings, string? StructuredJson, bool? AbnormalFlag, DateTimeOffset? PerformedAt);
