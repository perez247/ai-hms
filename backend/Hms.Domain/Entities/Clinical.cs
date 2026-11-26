using Hms.Domain.Common;
using Hms.Domain.Enums;

namespace Hms.Domain.Entities;

public class Appointment : Entity
{
    public string AppointmentNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid DoctorId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? Reason { get; set; }
    public FundingSource FundingSource { get; set; } = FundingSource.Private;
    public Guid? HmoEnrollmentId { get; set; }
    public PatientHmoEnrollment? HmoEnrollment { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public DateTimeOffset? CheckedInAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Inpatient stay aggregate. Each consultation during the stay is a separate
/// <see cref="Encounter"/>; discharge is gated by Finance clearance on the final bill.
/// </summary>
public class Admission : Entity
{
    public string AdmissionNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid AttendingDoctorId { get; set; }
    public Guid? HmoEnrollmentId { get; set; }
    public PatientHmoEnrollment? HmoEnrollment { get; set; }
    public WardType WardType { get; set; }
    public string? BedNumber { get; set; }
    public DateTimeOffset AdmittedAt { get; set; }
    public DateTimeOffset? ExpectedDischargeAt { get; set; }
    public DateTimeOffset? DischargedAt { get; set; }
    public AdmissionStatus Status { get; set; } = AdmissionStatus.Active;
    public string? DischargeSummary { get; set; }
    public Guid? DischargedBy { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
    public ICollection<BedDay> BedDays { get; set; } = new List<BedDay>();
}

/// <summary>
/// One clinical interaction. Outpatient encounters stand alone; inpatient encounters belong to an <see cref="Admission"/>.
/// </summary>
public class Encounter : Entity
{
    public string EncounterNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public EncounterType Type { get; set; }
    public bool IsEmergency { get; set; }
    public EncounterStatus Status { get; set; } = EncounterStatus.Open;
    public FundingSource FundingSource { get; set; } = FundingSource.Private;
    public Guid? HmoEnrollmentId { get; set; }
    public PatientHmoEnrollment? HmoEnrollment { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public string? PresentingComplaint { get; set; }
    public int? PresentingComplaintDurationDays { get; set; }
    public string? HistoryOfPresentingComplaint { get; set; }
    public string? PastMedicalSurgicalHistory { get; set; }
    public string? FamilyHistory { get; set; }
    public string? Examination { get; set; }
    /// <summary>Ancillary clerking fields captured by the structured form (ROS, allergies, plan advice…).</summary>
    public string? ClerkingDetails { get; set; }
    public string? Diagnosis { get; set; }
    public EncounterDisposition? Disposition { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<InvestigationOrder> Investigations { get; set; } = new List<InvestigationOrder>();
    public ICollection<SurgeryCase> Surgeries { get; set; } = new List<SurgeryCase>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}

public class Vital : Entity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid NurseId { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal? TemperatureC { get; set; }
    public int? Systolic { get; set; }
    public int? Diastolic { get; set; }
    public int? Pulse { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? Spo2 { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public int? PainScore { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Persisted nightly bed charge; one row per service date, created at discharge request.</summary>
public class BedDay : Entity
{
    public Guid AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public DateOnly ServiceDate { get; set; }
    public WardType WardType { get; set; }
    public decimal Rate { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
