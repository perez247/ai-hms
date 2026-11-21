using System.Data;
using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Reads PostgreSQL sequences to produce human-readable document numbers.</summary>
public sealed class NumberGenerator(HmsDbContext db) : INumberGenerator
{
    public async Task<string> NextAsync(string sequenceName, string prefix, CancellationToken cancellationToken = default)
    {
        if (!sequenceName.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
            throw new ArgumentException("Invalid sequence name.", nameof(sequenceName));

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT nextval('clinic.{sequenceName}')";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        var next = Convert.ToInt64(result);
        return $"{prefix}-{DateTime.UtcNow:yyyy}-{next:D6}";
    }
}
