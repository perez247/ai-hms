using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

/// <summary>
/// Pre-procedure fitness clearance. The Nurse captures patient details, flexible investigation
/// results and the signed certificate, then submits it for a Doctor decision. Finance manages
/// payment independently after approval.
/// </summary>
public class FitnessCertificate : Entity
{
    public string CertificateNumber { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string? PatientNumber { get; set; }
    public string PersonFullName { get; set; } = string.Empty;
    public int PatientAge { get; set; }
    public Sex PersonSex { get; set; }
    public string Phone { get; set; } = string.Empty;
    public FitnessPaymentType PaymentType { get; set; } = FitnessPaymentType.Private;
    public Guid? HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public Guid? HmoEnrollmentId { get; set; }
    public PatientHmoEnrollment? HmoEnrollment { get; set; }
    public DateOnly FitnessDate { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public FitnessStatus Status { get; set; } = FitnessStatus.AwaitingNurse;
    public FitnessPaymentStatus PaymentStatus { get; set; } = FitnessPaymentStatus.Unpaid;

    public string? CertificateFileName { get; set; }
    public string? CertificateContentType { get; set; }
    public string? CertificateStorageKey { get; set; }
    public DateTimeOffset? CertificateUploadedAt { get; set; }
    public Guid? CertificateUploadedBy { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? PaymentUpdatedAt { get; set; }
    public Guid? PaymentUpdatedBy { get; set; }

    public Guid? IssuedBy { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<FitnessInvestigationResult> InvestigationResults { get; set; } = new List<FitnessInvestigationResult>();
}

/// <summary>One free-form investigation finding captured by the Nurse.</summary>
public class FitnessInvestigationResult : Entity
{
    public Guid FitnessCertificateId { get; set; }
    public FitnessCertificate? FitnessCertificate { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
