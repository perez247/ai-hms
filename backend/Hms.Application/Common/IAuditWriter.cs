namespace Hms.Application.Common;

public interface IAuditWriter
{
    Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        Guid? patientId = null,
        Guid? encounterId = null,
        Guid? billId = null,
        object? before = null,
        object? after = null,
        CancellationToken cancellationToken = default);
}
