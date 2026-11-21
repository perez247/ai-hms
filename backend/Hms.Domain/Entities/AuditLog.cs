using Hms.Domain.Common;

namespace Hms.Domain.Entities;

/// <summary>Append-only audit row for monetary and clinical state changes.</summary>
public class AuditLog : Entity
{
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? BillId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
