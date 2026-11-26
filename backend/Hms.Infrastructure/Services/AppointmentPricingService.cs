using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>
/// Appointment fees are catalogue prices: Consultation/Emergency stock items with one
/// <see cref="Domain.Entities.ItemPrice"/> per payer (Private sentinel or a specific HMO).
/// A missing HMO tier falls back to the Private row, then to the legacy flat fee configuration.
/// </summary>
public sealed class AppointmentPricingService(HmsDbContext db) : IAppointmentPricingService
{
    public async Task<AppointmentPriceQuoteDto> QuoteAsync(string? reason, FundingSource fundingSource,
        Guid? hmoEnrollmentId, CancellationToken cancellationToken = default)
    {
        // Legacy appointments may carry free text; treat anything unrecognised as a consultation.
        var normalizedReason = AppointmentReasons.IsValid(reason) ? reason! : AppointmentReasons.Consultation;
        var payerId = Payers.PrivateId;
        var payerName = Payers.PrivateName;

        if (fundingSource == FundingSource.Hmo)
        {
            if (hmoEnrollmentId is null)
                throw new AppConflictException("Select the patient's HMO enrollment to price this appointment.");

            var enrollment = await db.PatientHmoEnrollments.AsNoTracking()
                .Where(x => x.Id == hmoEnrollmentId.Value)
                .Select(x => new { x.HmoCompanyId, Name = x.HmoCompany != null ? x.HmoCompany.Name : "HMO" })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new AppConflictException("The selected HMO enrollment was not found.");

            payerId = enrollment.HmoCompanyId;
            payerName = enrollment.Name;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var code = normalizedReason == AppointmentReasons.Emergency ? "EMERGENCY" : "CONSULT";
        var item = await db.StockItems.AsNoTracking()
            .Where(x => x.IsActive && x.Code == code)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        decimal unitPrice;
        var coverage = 0;
        var configured = false;

        if (item is not null)
        {
            var price = await db.ItemPrices.AsNoTracking()
                .Where(x => x.StockItemId == item.Id && x.PayerId == payerId && x.IsActive
                    && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (price is null && fundingSource == FundingSource.Hmo)
            {
                // Every HMO tier still gets a price even before its negotiated row is configured.
                price = await db.ItemPrices.AsNoTracking()
                    .Where(x => x.StockItemId == item.Id && x.PayerId == Payers.PrivateId && x.IsActive
                        && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
                    .OrderByDescending(x => x.EffectiveFrom)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (price is not null)
            {
                unitPrice = price.UnitPrice;
                coverage = fundingSource == FundingSource.Hmo ? price.CoveragePercent : 0;
                configured = true;
            }
            else
            {
                unitPrice = await LegacyFeeAsync(normalizedReason, cancellationToken);
            }
        }
        else
        {
            unitPrice = await LegacyFeeAsync(normalizedReason, cancellationToken);
        }

        var hmoAmount = Math.Round(unitPrice * coverage / 100m, 2, MidpointRounding.AwayFromZero);
        return new AppointmentPriceQuoteDto(normalizedReason, fundingSource, hmoEnrollmentId, payerName,
            unitPrice, coverage, hmoAmount, unitPrice - hmoAmount, configured);
    }

    private async Task<decimal> LegacyFeeAsync(string reason, CancellationToken cancellationToken)
    {
        var feeCode = reason == AppointmentReasons.Emergency ? FeeCode.Emergency : FeeCode.Appointment;
        var amount = await db.FeeConfigurations.AsNoTracking()
            .Where(x => x.Code == feeCode && x.IsActive)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (decimal?)x.Amount)
            .FirstOrDefaultAsync(cancellationToken);
        return amount ?? throw new AppConflictException($"No price is configured for {reason} appointments.");
    }
}
