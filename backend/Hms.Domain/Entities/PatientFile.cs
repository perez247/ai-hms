using Hms.Domain.Common;

namespace Hms.Domain.Entities;

/// <summary>Metadata for a file attached to a patient profile; bytes live in file storage.</summary>
public class PatientFile : Entity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
