namespace Hms.Application.DTOs;

public sealed record AuditLogDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId,
    string? ActorRole,
    string Action,
    string EntityType,
    string EntityId,
    Guid? PatientId,
    Guid? EncounterId,
    Guid? BillId,
    string? BeforeJson,
    string? AfterJson);
