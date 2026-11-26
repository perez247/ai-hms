using Hms.Application.Common;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Verifies that a payer is valid for the patient and resolves that payer's effective price row.</summary>
public sealed class FundingService(HmsDbContext db) : IFundingService
{
    public async Task<FundingContext> VerifyAsync(FundingSource source, Guid? hmoEnrollmentId, Guid patientId,
        DateOnly serviceDate, CancellationToken cancellationToken = default)
    {
        if (source == FundingSource.Private)
        {
            if (hmoEnrollmentId is not null)
                throw new AppConflictException("A private funding source cannot carry an HMO enrollment.");
            return new FundingContext(source, null, null);
        }

        if (hmoEnrollmentId is null)
            throw new AppConflictException("Select an HMO enrollment for HMO-funded services.");

        var enrollment = await db.PatientHmoEnrollments.Include(x => x.HmoCompany)
            .SingleOrDefaultAsync(x => x.Id == hmoEnrollmentId && x.PatientId == patientId, cancellationToken)
            ?? throw new AppConflictException("The selected HMO enrollment does not belong to this patient.");
        if (!enrollment.IsActive || enrollment.HmoCompany is null || !enrollment.HmoCompany.IsActive)
            throw new AppConflictException("The selected HMO coverage is not active.");
        if (serviceDate < enrollment.ValidFrom || (enrollment.ValidTo is not null && serviceDate > enrollment.ValidTo))
            throw new AppConflictException("The selected HMO coverage does not cover this service date.");
        var contract = await db.HmoContracts.AsNoTracking()
            .AnyAsync(x => x.HmoCompanyId == enrollment.HmoCompanyId && x.Status == HmoContractStatus.Active
                && x.StartDate <= serviceDate && x.EndDate >= serviceDate, cancellationToken);
        if (!contract)
            throw new AppConflictException("The selected HMO has no active contract covering this service date.");

        return new FundingContext(source, enrollment.Id, enrollment.HmoCompanyId);
    }

    public async Task<ItemPrice?> ResolvePriceAsync(Guid stockItemId, FundingContext funding, DateOnly serviceDate,
        CancellationToken cancellationToken = default)
    {
        var query = db.ItemPrices.AsNoTracking().Where(x => x.StockItemId == stockItemId && x.IsActive
            && x.EffectiveFrom <= serviceDate && (x.EffectiveTo == null || x.EffectiveTo >= serviceDate));
        query = funding.Source == FundingSource.Private
            ? query.Where(x => x.FundingSource == FundingSource.Private)
            : query.Where(x => x.FundingSource == FundingSource.Hmo && x.HmoCompanyId == funding.HmoCompanyId);
        return await query.OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<decimal> PrivatePriceAsync(Guid stockItemId, CancellationToken cancellationToken = default) =>
        db.ItemPrices.AsNoTracking()
            .Where(x => x.StockItemId == stockItemId && x.FundingSource == FundingSource.Private && x.IsActive)
            .OrderByDescending(x => x.EffectiveFrom).Select(x => x.UnitPrice).FirstOrDefaultAsync(cancellationToken);
}
