using System.Globalization;
using System.Text;
using System.Text.Json;
using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Versioned HMO tariff CSV import/export.</summary>
public sealed partial class AdminService
{

    /// <summary>Supersedes the previous active tariff and imports valid CSV rows into a new version.</summary>
    public async Task<TariffImportResultDto> ImportTariffAsync(Guid hmoCompanyId, string fileName, Stream csv,
        DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken = default)
    {
        if (!await db.HmoCompanies.AnyAsync(x => x.Id == hmoCompanyId, cancellationToken))
            throw new AppNotFoundException("HMO company not found.");
        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
            throw new AppConflictException("Tariff end date must be after the start date.");

        var existingActive = await db.HmoTariffs.Where(x => x.HmoCompanyId == hmoCompanyId && x.Status == TariffStatus.Active).ToListAsync(cancellationToken);
        foreach (var old in existingActive) old.Status = TariffStatus.Superseded;
        if (existingActive.Count > 0) await db.SaveChangesAsync(cancellationToken);
        var version = (await db.HmoTariffs.Where(x => x.HmoCompanyId == hmoCompanyId).MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;
        var tariff = new HmoTariff
        {
            HmoCompanyId = hmoCompanyId,
            Name = $"{fileName} v{version}",
            Version = version,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Status = TariffStatus.Active,
            SourceFileName = fileName,
            ImportedBy = user.RequireId()
        };

        var errors = new List<string>();
        using var reader = new StreamReader(csv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var first = true;
        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = ParseCsvLine(line);
            if (first)
            {
                first = false;
                if (cells.Count > 0 && cells[0].Trim().Equals("Code", StringComparison.OrdinalIgnoreCase)) continue;
            }
            if (cells.Count < 7)
            {
                errors.Add($"Line {lineNumber}: expected 7 columns.");
                continue;
            }
            if (!decimal.TryParse(cells[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
            {
                errors.Add($"Line {lineNumber}: invalid unit price.");
                continue;
            }
            if (!decimal.TryParse(cells[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var coverage) || coverage is < 0 or > 100)
            {
                errors.Add($"Line {lineNumber}: coverage must be between 0 and 100.");
                continue;
            }
            if (!Enum.TryParse<TariffCategory>(cells[2], ignoreCase: true, out var category))
            {
                errors.Add($"Line {lineNumber}: unknown category '{cells[2]}'.");
                continue;
            }
            tariff.Items.Add(new HmoTariffItem
            {
                Code = cells[0].Trim(),
                Description = cells[1].Trim(),
                Category = category,
                Unit = cells[3].Trim(),
                UnitPrice = price,
                CoveragePercent = coverage,
                RequiresPriorAuthorization = bool.TryParse(cells[6], out var req) && req
            });
        }

        if (tariff.Items.Count == 0)
            throw new AppConflictException("The tariff file contained no valid rows. " + string.Join(" ", errors));
        db.HmoTariffs.Add(tariff);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("admin.tariff-imported", nameof(HmoTariff), tariff.Id.ToString(),
            after: new { tariff.Version, Items = tariff.Items.Count, errors }, cancellationToken: cancellationToken);
        return new TariffImportResultDto(tariff.Id, tariff.Version, tariff.Items.Count, errors);
    }

    public async Task<byte[]> ExportTariffAsync(Guid hmoCompanyId, Guid tariffId, string? category, string? code, CancellationToken cancellationToken = default)
    {
        var tariff = await db.HmoTariffs.AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == tariffId && x.HmoCompanyId == hmoCompanyId, cancellationToken)
            ?? throw new AppNotFoundException("Tariff not found.");
        var query = tariff.Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<TariffCategory>(category, true, out var parsed))
            query = query.Where(x => x.Category == parsed);
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(x => x.Code.Contains(code, StringComparison.OrdinalIgnoreCase));
        var sb = new StringBuilder();
        sb.AppendLine("Code,Description,Category,Unit,UnitPrice,CoveragePercent,RequiresPriorAuthorization");
        foreach (var item in query.OrderBy(x => x.Code))
            sb.AppendLine(string.Join(',', Csv(item.Code), Csv(item.Description), item.Category,
                Csv(item.Unit), item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                item.CoveragePercent.ToString("0.##", CultureInfo.InvariantCulture), item.RequiresPriorAuthorization));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else current.Append(ch);
        }
        cells.Add(current.ToString());
        return cells;
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }
}
