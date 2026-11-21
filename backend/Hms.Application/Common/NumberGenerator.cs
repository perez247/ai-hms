namespace Hms.Application.Common;

/// <summary>Port for the PostgreSQL human-readable number sequences.</summary>
public interface INumberGenerator
{
    Task<string> NextAsync(string sequenceName, string prefix, CancellationToken cancellationToken = default);
}
