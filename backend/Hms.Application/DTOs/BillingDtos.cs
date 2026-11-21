using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record BillLineDto(
    Guid Id,
    BillLineCategory Category,
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
    HmoReceivableDto? Receivable);

public sealed record ApproveBillRequest(string? Notes);
public sealed record NegotiateBillRequest(decimal DiscountAmount, string Reason);
public sealed record PaymentRequest(decimal Amount, PaymentMethod Method, string? Reference, string? IdempotencyKey, string? Notes);
public sealed record AccountBookEntryRequest(AccountEntryType EntryType, string Category, decimal Amount, string Description, DateTimeOffset? OccurredAt);
public sealed record AccountBookEntryDto(Guid Id, string EntryNumber, AccountEntryType EntryType, string Category, decimal Amount, string Currency, DateTimeOffset OccurredAt, string Description, Guid? BillId, Guid? PaymentId);
