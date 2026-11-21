using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IBillingService
{
    Task<Bill> CreateAdministrativeBillAsync(Guid? patientId, BillKind kind, FeeCode feeCode, Guid sourceId,
        ChargeSourceType sourceType, string description, CancellationToken cancellationToken = default);
    Task<Bill> GetOrCreateEncounterBillAsync(Encounter encounter, CancellationToken cancellationToken = default);
    Task<Bill> GetOrCreateAdmissionBillAsync(Admission admission, CancellationToken cancellationToken = default);
    Task<BillLine> AddChargeAsync(Guid billId, BillLineCategory category, string description, decimal quantity,
        decimal unitPrice, ChargeSourceType sourceType, Guid sourceId, string sourceKey, Guid? encounterId,
        Guid? admissionId, string? tariffCode, CancellationToken cancellationToken = default);
    Task<Bill> CommitAsync(Guid billId, CancellationToken cancellationToken = default);
    Task<Bill> RecalculateAsync(Guid billId, CancellationToken cancellationToken = default);
    Task<PagedResult<BillDto>> SearchAsync(BillStatus? status, Guid? patientId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BillDto> GetAsync(Guid billId, CancellationToken cancellationToken = default);
    Task<Bill> RouteToHmoAsync(Guid billId, CancellationToken cancellationToken = default);
    Task<Bill> ApproveAsync(Guid billId, string? notes, CancellationToken cancellationToken = default);
    Task<Bill> NegotiateAsync(Guid billId, NegotiateBillRequest request, CancellationToken cancellationToken = default);
    Task<Bill> RecordPaymentAsync(Guid billId, PaymentRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AccountBookEntryDto>> GetAccountBookAsync(DateOnly? from, DateOnly? to, AccountEntryType? type, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AccountBookEntryDto> AddExpenseAsync(AccountBookEntryRequest request, CancellationToken cancellationToken = default);
}
