using Hms.Domain.Common;

namespace Hms.Domain.Entities;

/// <summary>Chronological shift handover entry written by a Nurse. Content is stored as rich HTML.</summary>
public class ShiftHandover : Entity
{
    public Guid NurseId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public string ContentHtml { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
