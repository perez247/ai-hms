using Hms.Application.DTOs;

namespace Hms.Application.Services;

public interface IStockImportService
{
    Task<ImportResultDto> ImportItemsAsync(Stream file, string fileName, CancellationToken cancellationToken = default);
    Task<ImportResultDto> ImportPricesAsync(Stream file, string fileName, CancellationToken cancellationToken = default);
    (byte[] Content, string ContentType, string FileName) BuildTemplate(string kind, string format);
}
