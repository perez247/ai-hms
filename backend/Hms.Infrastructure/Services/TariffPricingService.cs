using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Resolves the newest active tariff items for an HMO into a code-keyed lookup.</summary>
public sealed class TariffPricingService(HmsDbContext db) : ITariffPricingService
{
    public async Task<Dictionary<string, HmoTariffItem>> ResolveAsync(Guid hmoCompanyId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await db.HmoTariffItems.AsNoTracking()
            .Include(x => x.HmoTariff)
            .Where(x => x.HmoTariff != null && x.HmoTariff.HmoCompanyId == hmoCompanyId
                && x.HmoTariff.Status == TariffStatus.Active && x.IsActive
                && x.HmoTariff.EffectiveFrom <= today
                && (x.HmoTariff.EffectiveTo == null || x.HmoTariff.EffectiveTo >= today))
            .OrderByDescending(x => x.HmoTariff!.EffectiveFrom)
            .ToListAsync(cancellationToken);
        return items.GroupBy(x => x.Code).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HmoTariffItem?> ResolveOneAsync(Guid hmoCompanyId, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var items = await ResolveAsync(hmoCompanyId, cancellationToken);
        return items.GetValueOrDefault(code);
    }
}
