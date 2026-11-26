using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

/// <summary>Legacy investigation order kept for the existing billing path; new orders use ServiceRequest.</summary>
public class InvestigationOrder : Entity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid DoctorId { get; set; }
    public Guid CatalogItemId { get; set; }
    public StockItem? CatalogItem { get; set; }
    public InvestigationStatus Status { get; set; } = InvestigationStatus.Ordered;
    public DateTimeOffset OrderedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PerformedAt { get; set; }
    public string? ReportText { get; set; }
    public string? ReportFileRef { get; set; }
    public string? ReferralDestination { get; set; }
    public Guid? PerformedBy { get; set; }
    public Guid? CreatedBy { get; set; }
    public ICollection<InvestigationParticipant> Participants { get; set; } = new List<InvestigationParticipant>();
}

public class InvestigationParticipant : Entity
{
    public Guid InvestigationOrderId { get; set; }
    public InvestigationOrder? InvestigationOrder { get; set; }
    public Guid UserId { get; set; }
    public string ProcedureRole { get; set; } = "Performed";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Legacy surgery case kept for the existing billing path; new orders use ServiceRequest.</summary>
public class SurgeryCase : Entity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid LeadSurgeonId { get; set; }
    public Guid CatalogItemId { get; set; }
    public StockItem? CatalogItem { get; set; }
    public SurgeryStatus Status { get; set; } = SurgeryStatus.Scheduled;
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? PerformedAt { get; set; }
    public string? Theater { get; set; }
    public string? ReportText { get; set; }
    public string? ReportFileRef { get; set; }
    public Guid? CreatedBy { get; set; }
    public ICollection<SurgeryParticipant> Participants { get; set; } = new List<SurgeryParticipant>();
}

public class SurgeryParticipant : Entity
{
    public Guid SurgeryCaseId { get; set; }
    public SurgeryCase? SurgeryCase { get; set; }
    public Guid UserId { get; set; }
    public string ProcedureRole { get; set; } = "Assistant";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
