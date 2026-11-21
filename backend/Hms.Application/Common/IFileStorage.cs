namespace Hms.Application.Common;

public sealed record StoredFile(string StorageKey, long SizeBytes);

/// <summary>Infrastructure port for opaque file storage; callers only persist the storage key.</summary>
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(string area, Guid ownerId, string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
