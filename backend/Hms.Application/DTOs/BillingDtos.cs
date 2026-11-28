using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record AddBillLineRequest(Guid StockItemId, decimal Quantity, string? Description);

public sealed record BillLineDto(
    Guid Id,
    BillLineCategory Category,
    StockItemType? ItemType,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal NetAmount,
    decimal HmoAmount,
    decimal PatientAmount,
    bool IsHmoCovered,
    Guid? TariffItemId,
    DateTimeOffset? TariffConfirmedAt,
    string? ReferralDestination,
    Guid? EncounterId,
    Guid? AdmissionId,
    DateTimeOffset ChargeDate,
    Guid? CreatedBy);

public sealed record PaymentDto(Guid Id, decimal Amount, PaymentMethod Method, string? Reference, DateTimeOffset ReceivedAt, Guid? RecordedBy);
public sealed record BillAdjustmentDto(Guid Id, decimal Amount, string Reason, AdjustmentStatus Status, DateTimeOffset CreatedAt, Guid? ApprovedBy);
public sealed record HmoReceivableDto(Guid Id, string PaCode, decimal Amount, decimal SettledAmount, HmoReceivableStatus Status, DateOnly? DueDate);

public sealed record BillDto(
    Guid Id,
    string BillNumber,
    BillKind Kind,
    BillStatus Status,
    Guid? PatientId,
    string? PatientNumber,
    string PatientName,
    PayerType PayerType,
    Guid? HmoCompanyId,
    string? HmoCompanyName,
    Guid? PriorAuthorizationId,
    string? PaCode,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal HmoPayableTotal,
    decimal PatientPayableTotal,
    decimal PaidTotal,
    decimal Balance,
    Guid? EncounterId,
    Guid? AdmissionId,
    Guid? AppointmentId,
    Guid? RegistrationPeriodId,
    Guid? FitnessCertificateId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CommittedAt,
    DateTimeOffset? HmoApprovedAt,
    DateTimeOffset? FinanceApprovedAt,
    DateTimeOffset? PaidAt,
    IReadOnlyList<BillLineDto> Lines,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<BillAdjustmentDto> Adjustments,
    IReadOnlyList<MedicationAdministrationDto>? MedicationAdministrations,
    HmoReceivableDto? Receivable,
    bool IsDebt,
    DebtOwner? DebtOwner,
    DateTimeOffset? DebtRecordedAt,
    string? DebtReason,
    DateTimeOffset? DebtSettledAt);

public sealed record MoveBillToDebtRequest(string? Reason);
public sealed record ApproveBillRequest(string? Notes);
public sealed record NegotiateBillRequest(decimal DiscountAmount, string Reason);
public sealed record PaymentRequest(decimal Amount, PaymentMethod Method, string? Reference, string? IdempotencyKey, string? Notes);
public sealed record AccountBookEntryRequest(AccountEntryType EntryType, string Category, decimal Amount, string Description, DateTimeOffset? OccurredAt, string? Reference);
public sealed record AccountBookEntryDto(Guid Id, string EntryNumber, AccountEntryType EntryType, string Direction, string Category, decimal Amount, string Currency, DateTimeOffset OccurredAt, string Description, string? Reference, string? Source, Guid? BillId, Guid? PaymentId);
public sealed record RecordCreditRequest(decimal Amount, string Source, DateTimeOffset? OccurredAt, string Description, string? Reference);
public sealed record AccountBookSummaryDto(decimal TotalCredits, decimal TotalDebits, decimal Balance, string Currency);

public sealed record CreateManualBillLineRequest(
    BillLineCategory Category,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string? TariffCode);

/// <summary>Finance-created bill. Selecting an HMO prices medication lines from that HMO's active tariff.</summary>
public sealed record CreateManualBillRequest(
    Guid PatientId,
    Guid? HmoCompanyId,
    Guid? HmoEnrollmentId,
    string? Description,
    IReadOnlyList<CreateManualBillLineRequest> Lines);

public sealed record ManualBillResultDto(BillDto Bill, IReadOnlyList<string> Warnings);
