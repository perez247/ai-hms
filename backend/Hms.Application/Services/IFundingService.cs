using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public sealed record FundingContext(FundingSource Source, Guid? HmoEnrollmentId, Guid? HmoCompanyId);

/// <summary>Validates payer selection and resolves the effective price row for an item.</summary>
public interface IFundingService
{
    Task<FundingContext> VerifyAsync(FundingSource source, Guid? hmoEnrollmentId, Guid patientId,
        DateOnly serviceDate, CancellationToken cancellationToken = default);

    Task<ItemPrice?> ResolvePriceAsync(Guid stockItemId, FundingContext funding, DateOnly serviceDate,
        CancellationToken cancellationToken = default);

    Task<decimal> PrivatePriceAsync(Guid stockItemId, CancellationToken cancellationToken = default);
}
