using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

/// <summary>
/// Resolves the appointment fee from the stock/price catalogue for the selected reason and payer.
/// </summary>
public interface IAppointmentPricingService
{
    Task<AppointmentPriceQuoteDto> QuoteAsync(string? reason, FundingSource fundingSource,
        Guid? hmoEnrollmentId, CancellationToken cancellationToken = default);
}
