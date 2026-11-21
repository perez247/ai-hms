using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Domain.Workflows;

/// <summary>
/// Explicit pre-procedure clearance workflow. Nurse submission moves Draft/Rejected to Doctor
/// review; only the Doctor can approve/reject; Finance payment is allowed only after approval.
/// </summary>
public static class FitnessStateMachine
{
    public static void EnsureCanCaptureClinicalInfo(FitnessCertificate record)
    {
        if (record.Status is not (FitnessStatus.AwaitingNurse or FitnessStatus.Rejected))
            throw new DomainRuleException("Only records awaiting Nurse review or rejected records can be edited.");
    }

    public static void EnsureCanSubmit(FitnessCertificate record)
    {
        EnsureCanCaptureClinicalInfo(record);
        if (string.IsNullOrWhiteSpace(record.CertificateStorageKey))
            throw new DomainRuleException("Upload the signed certificate before submitting.");
    }

    public static void EnsureCanDecide(FitnessCertificate record)
    {
        if (record.Status != FitnessStatus.AwaitingDoctor)
            throw new DomainRuleException("Fitness record is not awaiting doctor review.");
    }

    public static void EnsureCanSetPayment(FitnessCertificate record)
    {
        if (record.Status != FitnessStatus.Approved)
            throw new DomainRuleException("Fitness payment can only be updated after doctor approval.");
    }
}
