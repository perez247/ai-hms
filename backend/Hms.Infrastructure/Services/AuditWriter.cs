using System.Text.Json;
using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Domain.Entities;

namespace Hms.Infrastructure.Services;

/// <summary>Adds immutable audit rows using the request identity and correlation context.</summary>
public sealed class AuditWriter(HmsDbContext db, ICurrentUser currentUser) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        Guid? patientId = null,
        Guid? encounterId = null,
        Guid? billId = null,
        object? before = null,
        object? after = null,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.Id,
            ActorRole = currentUser.Role?.ToString(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PatientId = patientId,
            EncounterId = encounterId,
            BillId = billId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            IpAddress = currentUser.IpAddress,
            UserAgent = currentUser.UserAgent
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
