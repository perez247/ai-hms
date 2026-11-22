using Hms.Domain.Entities;

namespace Hms.Application.Services;

/// <summary>
/// Resolves the currently active negotiated prices for an HMO. Shared by HMO, Finance and Pharmacy
/// so the same tariff data drives registration, bill creation and price comparison.
/// </summary>
public interface ITariffPricingService
{
    Task<Dictionary<string, HmoTariffItem>> ResolveAsync(Guid hmoCompanyId, CancellationToken cancellationToken = default);
    Task<HmoTariffItem?> ResolveOneAsync(Guid hmoCompanyId, string code, CancellationToken cancellationToken = default);
}
