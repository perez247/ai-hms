using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Domain.Workflows;

namespace Hms.UnitTests;

public sealed class FitnessStateMachineTests
{
    [Fact]
    public void Submission_does_not_require_an_uploaded_certificate()
    {
        var record = new FitnessCertificate { Status = FitnessStatus.AwaitingNurse };
        FitnessStateMachine.EnsureCanSubmit(record);
    }

    [Fact]
    public void Approved_or_pending_records_cannot_be_resubmitted()
    {
        var record = new FitnessCertificate { Status = FitnessStatus.AwaitingDoctor, CertificateStorageKey = "x" };
        Assert.Throws<DomainRuleException>(() => FitnessStateMachine.EnsureCanSubmit(record));
        record.Status = FitnessStatus.Approved;
        Assert.Throws<DomainRuleException>(() => FitnessStateMachine.EnsureCanSubmit(record));
    }

    [Fact]
    public void Only_pending_records_can_be_decided()
    {
        var record = new FitnessCertificate { Status = FitnessStatus.AwaitingNurse };
        Assert.Throws<DomainRuleException>(() => FitnessStateMachine.EnsureCanDecide(record));
        record.Status = FitnessStatus.AwaitingDoctor;
        FitnessStateMachine.EnsureCanDecide(record);
    }

    [Fact]
    public void Finance_payment_requires_doctor_approval()
    {
        var record = new FitnessCertificate { Status = FitnessStatus.AwaitingDoctor };
        Assert.Throws<DomainRuleException>(() => FitnessStateMachine.EnsureCanSetPayment(record));
        record.Status = FitnessStatus.Approved;
        FitnessStateMachine.EnsureCanSetPayment(record);
    }
}
