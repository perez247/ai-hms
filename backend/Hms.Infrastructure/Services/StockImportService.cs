using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>
/// CSV/XLSX bulk upload for stock items and prices. Parses OOXML in-process (no external package),
/// validates every row and returns row-level errors while importing only valid rows.
/// </summary>
public sealed class StockImportService(HmsDbContext db, ICurrentUser user, IAuditWriter audit) : IStockImportService
{
    private static readonly string[] ItemHeaders = ["code", "name", "type", "quantity"];
    private static readonly string[] PriceHeaders = ["itemcode", "payeridorname", "price"];

    public async Task<ImportResultDto> ImportItemsAsync(Stream file, string fileName, CancellationToken cancellationToken = default)
    {
        var rows = await ReadAsync(file, fileName, ItemHeaders, cancellationToken);
        var errors = new List<ImportErrorDto>();
        var existing = await db.StockItems.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x, cancellationToken);
        var imported = 0;

        foreach (var row in rows)
        {
            if (row.IsHeader) continue;
            var code = row.Get("code").Trim();
            var name = row.Get("name").Trim();
            var typeText = row.Get("type").Trim();
            var quantityText = row.Get("quantity").Trim();
            if (code.Length == 0) { errors.Add(row.Error("code", "Code is required.")); continue; }
            if (name.Length == 0) { errors.Add(row.Error("name", "Name is required.")); continue; }
            if (!Enum.TryParse<StockItemType>(typeText, true, out var type))
            { errors.Add(row.Error("type", "Type must be Medication, Test or Procedure.")); continue; }
            if (!TryDecimal(quantityText, out var quantity) || quantity < 0)
            { errors.Add(row.Error("quantity", "Quantity must be a non-negative number.")); continue; }

            if (existing.TryGetValue(code, out var item))
            {
                item.Name = name;
                item.ItemType = type;
                item.Quantity = quantity;
                item.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                item = new StockItem { Code = code, Name = name, ItemType = type, Quantity = quantity, CreatedBy = user.RequireId() };
                db.StockItems.Add(item);
                existing[code] = item;
            }
            imported++;
        }
        if (imported > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("stock.imported", nameof(StockItem), fileName,
                after: new { fileName, imported }, cancellationToken: cancellationToken);
        }
        return new ImportResultDto(rows.Count(x => !x.IsHeader), imported, errors);
    }

    public async Task<ImportResultDto> ImportPricesAsync(Stream file, string fileName, CancellationToken cancellationToken = default)
    {
        var rows = await ReadAsync(file, fileName, PriceHeaders, cancellationToken);
        var errors = new List<ImportErrorDto>();
        var items = await db.StockItems.AsNoTracking().ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var hmos = await db.HmoCompanies.AsNoTracking().Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var prices = await db.ItemPrices.ToListAsync(cancellationToken);
        var imported = 0;

        foreach (var row in rows)
        {
            if (row.IsHeader) continue;
            var itemCode = row.Get("itemcode").Trim();
            var payerText = row.Get("payeridorname").Trim();
            if (!items.TryGetValue(itemCode, out var item))
            { errors.Add(row.Error("itemCode", $"No stock item with code '{itemCode}'.")); continue; }
            if (!TryResolvePayer(payerText, hmos, out var payerId, out var hmoId, out var source))
            { errors.Add(row.Error("payerIdOrName", $"Unknown payer '{payerText}'. Use Private or an active HMO id/name.")); continue; }
            if (!TryDecimal(row.Get("price"), out var price) || price < 0)
            { errors.Add(row.Error("price", "Price must be a non-negative number.")); continue; }

            var existing = prices.FirstOrDefault(x => x.StockItemId == item.Id && x.PayerId == payerId);
            if (existing is null)
            {
                existing = new ItemPrice
                {
                    StockItemId = item.Id, PayerId = payerId, FundingSource = source, HmoCompanyId = hmoId,
                    EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), IsActive = true
                };
                db.ItemPrices.Add(existing);
                prices.Add(existing);
            }
            existing.UnitPrice = price;
            imported++;
        }
        if (imported > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("stock.prices-imported", nameof(ItemPrice), fileName,
                after: new { fileName, imported }, cancellationToken: cancellationToken);
        }
        return new ImportResultDto(rows.Count(x => !x.IsHeader), imported, errors);
    }

    public (byte[] Content, string ContentType, string FileName) BuildTemplate(string kind, string format)
    {
        var headers = kind.Equals("prices", StringComparison.OrdinalIgnoreCase) ? PriceHeaders : ItemHeaders;
        var sample = kind.Equals("prices", StringComparison.OrdinalIgnoreCase)
            ? new[] { "PARA500", "Private", "500" }
            : new[] { "PARA500", "Paracetamol 500mg", "Medication", "1000" };
        var rows = new[] { headers, sample, sample };
        var isCsv = !format.Equals("xlsx", StringComparison.OrdinalIgnoreCase);
        if (isCsv)
        {
            var text = string.Join("\n", rows.Select(row => string.Join(",", row.Select(Csv)))) + "\n";
            return (Encoding.UTF8.GetBytes(text), "text/csv", $"stock-{kind}-template.csv");
        }
        return (BuildXlsx(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"stock-{kind}-template.xlsx");
    }

    // ---- helpers -----------------------------------------------------------

    private static bool TryResolvePayer(string text, IReadOnlyDictionary<string, HmoCompany> hmos,
        out Guid payerId, out Guid? hmoId, out FundingSource source)
    {
        if (text.Equals(Payers.PrivateName, StringComparison.OrdinalIgnoreCase)
            || text.Equals("SelfPay", StringComparison.OrdinalIgnoreCase)
            || Guid.TryParse(text, out var parsed) && parsed == Payers.PrivateId)
        {
            payerId = Payers.PrivateId; hmoId = null; source = FundingSource.Private; return true;
        }
        if (Guid.TryParse(text, out payerId))
        {
            var resolved = payerId;
            if (hmos.Values.Any(x => x.Id == resolved))
            {
                hmoId = resolved; source = FundingSource.Hmo; return true;
            }
        }
        if (hmos.TryGetValue(text, out var company))
        {
            payerId = company.Id; hmoId = company.Id; source = FundingSource.Hmo; return true;
        }
        payerId = Guid.Empty; hmoId = null; source = FundingSource.Private; return false;
    }

    private static bool TryDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static async Task<List<CsvRow>> ReadAsync(Stream file, string fileName, string[] expectedHeaders, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (bytes.Length == 0) throw new AppConflictException("The upload file is empty.");
        var cells = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? ReadXlsx(bytes)
            : ReadCsv(bytes);
        if (cells.Count == 0) throw new AppConflictException("The upload contains no rows.");
        var header = cells[0];
        var missing = expectedHeaders.Where(h => !header.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
            throw new AppConflictException($"Missing required column(s): {string.Join(", ", missing)}.");
        return cells.Select((row, index) => new CsvRow(index + 1, header, row)).ToList();
    }

    private static List<string[]> ReadCsv(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
        return Regex.Split(text, "\r?\n").Where(line => line.Trim().Length > 0).Select(ParseCsvLine).ToList();
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { values.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        values.Add(current.ToString());
        return values.ToArray();
    }

    private static List<string[]> ReadXlsx(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var strings = new List<string>();
        var shared = zip.GetEntry("xl/sharedStrings.xml");
        if (shared is not null)
        {
            using var reader = new StreamReader(shared.Open());
            var doc = XDocument.Parse(reader.ReadToEnd());
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            strings.AddRange(doc.Descendants(ns + "si").Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value))));
        }
        var sheet = zip.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new AppConflictException("The XLSX file has no first worksheet.");
        using var sheetReader = new StreamReader(sheet.Open());
        var sheetDoc = XDocument.Parse(sheetReader.ReadToEnd());
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var result = new List<string[]>();
        foreach (var row in sheetDoc.Descendants(main + "row"))
        {
            var values = new List<string>();
            foreach (var cell in row.Elements(main + "c"))
            {
                var type = (string?)cell.Attribute("t");
                var valueElement = cell.Element(main + "v");
                var inline = cell.Element(main + "is");
                string value;
                if (type == "s" && valueElement is not null && int.TryParse(valueElement.Value, out var index) && index < strings.Count)
                    value = strings[index];
                else if (inline is not null) value = string.Concat(inline.Descendants(main + "t").Select(t => t.Value));
                else value = valueElement?.Value ?? string.Empty;
                values.Add(value);
            }
            if (values.Any(v => v.Trim().Length > 0)) result.Add(values.ToArray());
        }
        return result;
    }

    private static byte[] BuildXlsx(string[][] rows)
    {
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var sheet = new XDocument(new XElement(main + "worksheet", new XElement(main + "sheetData",
            rows.Select((row, r) => new XElement(main + "row", new XAttribute("r", r + 1),
                row.Select((cell, c) =>
                {
                    var reference = $"{(char)('A' + c)}{r + 1}";
                    return new XElement(main + "c", new XAttribute("r", reference), new XAttribute("t", "inlineStr"),
                        new XElement(main + "is", new XElement(main + "t", cell)));
                }))))));
        var workbook = new XDocument(new XElement(main + "workbook", new XElement(main + "sheets",
            new XElement(main + "sheet", new XAttribute("name", "Stock"), new XAttribute("sheetId", 1),
                new XAttribute(rel + "id", "rId1")))));
        var workbookRels = new XDocument(new XElement(XName.Get("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships"),
            new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
                new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", "worksheets/sheet1.xml"))));
        var rootRels = new XDocument(new XElement(XName.Get("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships"),
            new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
                new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                new XAttribute("Target", "xl/workbook.xml"))));
        var contentTypes = new XDocument(new XElement(XName.Get("Types", "http://schemas.openxmlformats.org/package/2006/content-types"),
            new XElement(XName.Get("Default", "http://schemas.openxmlformats.org/package/2006/content-types"), new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(XName.Get("Default", "http://schemas.openxmlformats.org/package/2006/content-types"), new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
            new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"), new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"), new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml", contentTypes);
            Add(zip, "_rels/.rels", rootRels);
            Add(zip, "xl/workbook.xml", workbook);
            Add(zip, "xl/_rels/workbook.xml.rels", workbookRels);
            Add(zip, "xl/worksheets/sheet1.xml", sheet);
        }
        return stream.ToArray();
    }

    private static void Add(ZipArchive zip, string path, XDocument doc)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open());
        writer.Write(doc);
    }

    private sealed record CsvRow(int Number, string[] Header, string[] Cells)
    {
        public bool IsHeader => Number == 1;
        public string Get(string column)
        {
            var index = Array.FindIndex(Header, h => h.Equals(column, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index < Cells.Length ? Cells[index] : string.Empty;
        }
        public ImportErrorDto Error(string column, string message) => new(Number, column, message);
    }
}
