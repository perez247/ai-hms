using Hms.Application.Common;
using Microsoft.Extensions.Options;

namespace Hms.Infrastructure.Services;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string RootPath { get; set; } = "App_Data/uploads";
}

/// <summary>
/// Local disk storage for certificates and patient attachments. Keys are relative paths;
/// generated names prevent user-controlled traversal and collisions.
/// </summary>
public sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<StoredFile> SaveAsync(string area, Guid ownerId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var safeExtension = Path.GetExtension(fileName);
        if (safeExtension.Length > 12) safeExtension = string.Empty;
        var relative = Path.Combine(Sanitize(area), ownerId.ToString("N"), $"{Guid.NewGuid():N}{safeExtension}");
        var fullPath = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, cancellationToken);
        return new StoredFile(relative.Replace('\\', '/'), target.Length);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, storageKey));
        if (!fullPath.StartsWith(_root, StringComparison.Ordinal) || !File.Exists(fullPath))
            throw new FileNotFoundException("Stored file was not found.", storageKey);
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')).ToLowerInvariant();
}
