using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

/// <summary>
/// The single source of orderable items. Medications carry stock quantities; tests and
/// procedures never do. Prices live in <see cref="ItemPrice"/>, one row per funding source.
/// </summary>
public class StockItem : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StockItemType ItemType { get; set; }
    public string? Category { get; set; }
    public string? GenericName { get; set; }
    public string? Form { get; set; }
    public string? Strength { get; set; }
    public string Unit { get; set; } = "unit";
    public decimal Quantity { get; set; }
    public string? TariffCode { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<ItemPrice> Prices { get; set; } = new List<ItemPrice>();
}

/// <summary>Price of a stock item for one payer category (the Private sentinel or a specific HMO).</summary>
public class ItemPrice : Entity
{
    public Guid StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public Guid PayerId { get; set; } = Payers.PrivateId;
    public FundingSource FundingSource { get; set; }
    public Guid? HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public decimal UnitPrice { get; set; }
    public int CoveragePercent { get; set; } = 100;
    public bool RequiresPriorAuthorization { get; set; }
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// A doctor's test or procedure order. Linked to the encounter for navigation; the price and payer
/// are snapshotted so later tariff changes never alter the order.
/// </summary>
public class ServiceRequest : Entity
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid StockItemId { get; set; }
    public StockItem? StockItem { get; set; }
    public StockItemType Kind { get; set; }
    public Guid DoctorId { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;
    public DateTimeOffset OrderedAt { get; set; } = DateTimeOffset.UtcNow;
    public FundingSource FundingSource { get; set; }
    public Guid? HmoEnrollmentId { get; set; }
    public Guid? HmoCompanyId { get; set; }
    public Guid? ItemPriceId { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public string? Priority { get; set; }
    public string? ClinicalNote { get; set; }
    public DateTimeOffset? PerformedAt { get; set; }
    public DateTimeOffset? BillLineCreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>Result/findings record, created with every request and still editable after encounter sign-off.</summary>
public class ServiceResult : Entity
{
    public Guid RequestId { get; set; }
    public ServiceRequest? Request { get; set; }
    public ServiceResultStatus Status { get; set; } = ServiceResultStatus.Pending;
    public string? ResultText { get; set; }
    public string? Findings { get; set; }
    public string? StructuredJson { get; set; }
    public bool? AbnormalFlag { get; set; }
    public DateTimeOffset? PerformedAt { get; set; }
    public Guid? EnteredById { get; set; }
    public DateTimeOffset? EnteredAt { get; set; }
    public Guid? SubmittedById { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>Append-only revision trail for result edits, including edits after sign-off.</summary>
public class ServiceResultRevision : Entity
{
    public Guid ResultId { get; set; }
    public ServiceResult? Result { get; set; }
    public int Version { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorRole { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
}
