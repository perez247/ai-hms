using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

public class Patient : Entity
{
    public string PatientNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public Sex Sex { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NextOfKinName { get; set; }
    public string? NextOfKinPhone { get; set; }
    public DateOnly RegistrationDate { get; set; }
    public DateOnly CurrentRegistrationExpiresAt { get; set; }
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<PatientRegistrationPeriod> RegistrationPeriods { get; set; } = new List<PatientRegistrationPeriod>();
    public ICollection<PatientHmoEnrollment> HmoEnrollments { get; set; } = new List<PatientHmoEnrollment>();

    public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ").Trim();
}

public class PatientRegistrationPeriod : Entity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal FeeAmount { get; set; }
    public bool IsCurrent { get; set; }
    public Guid? RenewalOfId { get; set; }
    public PatientRegistrationPeriod? RenewalOf { get; set; }
    public Guid? ProcessedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PatientHmoEnrollment : Entity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid HmoCompanyId { get; set; }
    public HmoCompany? HmoCompany { get; set; }
    public string HmoMemberId { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
