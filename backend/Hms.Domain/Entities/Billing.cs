using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

/// <summary>
/// Header for one billing context (administrative fee, outpatient encounter or inpatient admission).
/// Lines accrue in <see cref="BillStatus.Draft"/>; Finance and HMO approval timestamps are the
/// authoritative evidence used by the state machine before a bill can become payable.
/// </summary>
public class Bill : Entity
{
    public string BillNumber { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public BillKind Kind { get; set; }
    public Guid? EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public Guid? RegistrationPeriodId { get; set; }
    public PatientRegistrationPeriod? RegistrationPeriod { get; set; }
    public Guid? FitnessCertificateId { get; set; }
    public FitnessCertificate? FitnessCertificate { get; set; }
    public PayerType PayerType { get; set; } = PayerType.SelfPay;
    public Guid? HmoEnrollmentId { get; set; }
    public PatientHmoEnrollment? HmoEnrollment { get; set; }
    public Guid? HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public Guid? PriorAuthorizationId { get; set; }
    public PriorAuthorization? PriorAuthorization { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;
    public string Currency { get; set; } = "NGN";
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal HmoPayableTotal { get; set; }
    public decimal PatientPayableTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public DateTimeOffset? CommittedAt { get; set; }
    public Guid? CommittedBy { get; set; }
    public DateTimeOffset? HmoSubmittedAt { get; set; }
    public DateTimeOffset? HmoApprovedAt { get; set; }
    public Guid? HmoApprovedBy { get; set; }
    public DateTimeOffset? FinanceApprovedAt { get; set; }
    public Guid? FinanceApprovedBy { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<BillLine> Lines { get; set; } = new List<BillLine>();
    public ICollection<BillAdjustment> Adjustments { get; set; } = new List<BillAdjustment>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<BillStatusHistory> StatusHistory { get; set; } = new List<BillStatusHistory>();
    public HmoReceivable? Receivable { get; set; }

    public decimal Balance => PatientPayableTotal + HmoPayableTotal - PaidTotal;
}

/// <summary>
/// Immutable-after-commit charge line. <see cref="SourceKey"/> is a unique idempotency key
/// tying the line back to its clinical or administrative source event.
/// </summary>
public class BillLine : Entity
{
    public Guid BillId { get; set; }
    public Bill? Bill { get; set; }
    public BillLineCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal HmoAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public bool IsHmoCovered { get; set; }
    public Guid? TariffItemId { get; set; }
    public HmoTariffItem? TariffItem { get; set; }
    public DateTimeOffset? TariffConfirmedAt { get; set; }
    public Guid? TariffConfirmedBy { get; set; }
    public DateTimeOffset? HmoCoverageReviewedAt { get; set; }
    public Guid? HmoCoverageReviewedBy { get; set; }
    public string? ReferralDestination { get; set; }
    public ChargeSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public string SourceKey { get; set; } = string.Empty;
    public Guid? EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public DateTimeOffset ChargeDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Audited negotiated discount applied to a bill's patient-payable portion.</summary>
public class BillAdjustment : Entity
{
    public Guid BillId { get; set; }
    public Bill? Bill { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AdjustmentStatus Status { get; set; } = AdjustmentStatus.Proposed;
    public Guid? RequestedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Patient or HMO money received against a bill; idempotency protects retries.</summary>
public class Payment : Entity
{
    public Guid BillId { get; set; }
    public Bill? Bill { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? RecordedBy { get; set; }
    public string? Notes { get; set; }
    public string? IdempotencyKey { get; set; }
}

/// <summary>Finance-created claim against an HMO, keyed by the encounter's PA code.</summary>
public class HmoReceivable : Entity
{
    public Guid BillId { get; set; }
    public Bill? Bill { get; set; }
    public Guid HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public Guid PriorAuthorizationId { get; set; }
    public PriorAuthorization? PriorAuthorization { get; set; }
    public string PaCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SettledAmount { get; set; }
    public HmoReceivableStatus Status { get; set; } = HmoReceivableStatus.Open;
    public DateOnly? DueDate { get; set; }
    public Guid? RecordedBy { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SettledAt { get; set; }
}

/// <summary>HMO prior-authorization record. HMO approval is blocked until this is issued and unexpired.</summary>
public class PriorAuthorization : Entity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public Guid HmoEnrollmentId { get; set; }
    public PatientHmoEnrollment? HmoEnrollment { get; set; }
    public Guid? EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public string PaCode { get; set; } = string.Empty;
    public PriorAuthorizationStatus Status { get; set; } = PriorAuthorizationStatus.Requested;
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? RequestedBy { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? Notes { get; set; }
}

public class BillStatusHistory : Entity
{
    public Guid BillId { get; set; }
    public Bill? Bill { get; set; }
    public BillStatus? FromStatus { get; set; }
    public BillStatus ToStatus { get; set; }
    public Guid? ActorId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AccountBookEntry : Entity
{
    public string EntryNumber { get; set; } = string.Empty;
    public AccountEntryType EntryType { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NGN";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string Description { get; set; } = string.Empty;
    public Guid? BillId { get; set; }
    public Bill? Bill { get; set; }
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid? HmoReceivableId { get; set; }
    public HmoReceivable? HmoReceivable { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
